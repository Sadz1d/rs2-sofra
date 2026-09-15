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
using Stripe;

namespace Sofra.API.Services;

public class PaymentService(
    AppDbContext dbContext,
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

    private static long ToStripeAmount(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
