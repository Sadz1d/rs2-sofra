using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Data;
using Sofra.API.DTOs.Payments;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Options;
using Sofra.API.Requests.Payments;
using Sofra.API.Services.Interfaces;
using Sofra.Shared.Events;
using Stripe;

namespace Sofra.API.Services;

public class PaymentService(
    AppDbContext dbContext,
    IEventPublisher eventPublisher,
    IOptions<StripeOptions> stripeOptions) : IPaymentService
{
    private readonly StripeOptions _stripeOptions = stripeOptions.Value;

    public async Task<PaymentIntentResponse> CreateIntentAsync(PaymentIntentRequest request, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Narudžba sa Id {request.OrderId} ne postoji.");

        if (!isStaff && order.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete platiti tuđu narudžbu.");
        }

        if (order.Payment is not null)
        {
            throw new BusinessException("Narudžba već ima plaćanje.");
        }

        var cardMethodId = await dbContext.PaymentMethods.Where(x => x.Code == "CARD").Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (cardMethodId == 0)
        {
            throw new BusinessException("Način plaćanja 'Kartica' nije podešen u šifarniku.");
        }

        PaymentIntent intent;
        try
        {
            var intentService = new PaymentIntentService();
            intent = await intentService.CreateAsync(new PaymentIntentCreateOptions
            {
                // Iznos se uvijek racuna na serveru iz narudzbe - klijent ga nikad ne salje.
                Amount = ToStripeAmount(order.Total),
                Currency = _stripeOptions.Currency,
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString(),
                    ["orderNumber"] = order.Number,
                },
            }, cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            throw new BusinessException($"Kreiranje Stripe plaćanja nije uspjelo: {ex.StripeError?.Message ?? ex.Message}");
        }

        var payment = new Payment
        {
            CreatedAt = DateTime.UtcNow,
            OrderId = order.Id,
            PaymentMethodId = cardMethodId,
            Amount = order.Total,
            Status = PaymentStatus.Pending,
            StripePaymentIntentId = intent.Id,
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PaymentIntentResponse(payment.Id, intent.ClientSecret, order.Total, _stripeOptions.Currency);
    }

    public async Task HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default)
    {
        Event stripeEvent;
        try
        {
            // throwOnApiVersionMismatch: false - Stripe nalog moze biti na drugoj API verziji od
            // one koju SDK ocekuje; validni webhook ne smije biti odbijen samo zbog te razlike.
            stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeOptions.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            throw new BusinessException("Nevažeći Stripe potpis.");
        }

        if (await dbContext.ProcessedStripeEvents.AnyAsync(x => x.StripeEventId == stripeEvent.Id, cancellationToken))
        {
            // Idempotentno - isti Stripe event je vec obradjen, ne mijenjaj stanje ponovo.
            return;
        }

        Payment? succeededPayment = null;

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded && stripeEvent.Data.Object is PaymentIntent succeededIntent)
        {
            var payment = await dbContext.Payments.Include(x => x.Order).ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.StripePaymentIntentId == succeededIntent.Id, cancellationToken);

            if (payment is not null && payment.Status != PaymentStatus.Succeeded)
            {
                payment.Status = PaymentStatus.Succeeded;
                payment.PaidAt = DateTime.UtcNow;
                succeededPayment = payment;
            }
        }
        else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed && stripeEvent.Data.Object is PaymentIntent failedIntent)
        {
            var payment = await dbContext.Payments.FirstOrDefaultAsync(x => x.StripePaymentIntentId == failedIntent.Id, cancellationToken);
            if (payment is not null)
            {
                payment.Status = PaymentStatus.Failed;
            }
        }

        dbContext.ProcessedStripeEvents.Add(new ProcessedStripeEvent { StripeEventId = stripeEvent.Id, ProcessedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (succeededPayment is not null)
        {
            // Objavljivanje ide tek nakon uspjesnog SaveChangesAsync, nikad unutar transakcije.
            await eventPublisher.PublishAsync(
                new PaymentSucceededEvent(
                    Guid.NewGuid(), DateTime.UtcNow,
                    succeededPayment.Id, succeededPayment.OrderId, succeededPayment.Order.Number,
                    succeededPayment.Order.UserId, succeededPayment.Order.User.Email ?? string.Empty,
                    $"{succeededPayment.Order.User.FirstName} {succeededPayment.Order.User.LastName}",
                    succeededPayment.Amount),
                EventRoutingKeys.PaymentSucceeded,
                cancellationToken);
        }
    }

    private static long ToStripeAmount(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
