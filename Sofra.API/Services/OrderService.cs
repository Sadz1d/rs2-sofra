using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Orders;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Hubs;
using Sofra.API.Hubs.Messages;
using Sofra.API.Options;
using Sofra.API.Requests.Orders;
using Sofra.API.Services.Interfaces;
using Sofra.Shared.Events;

namespace Sofra.API.Services;

public class OrderService(
    AppDbContext dbContext,
    IOrderStateMachine stateMachine,
    IDiningTableStatusService diningTableStatusService,
    IEventPublisher eventPublisher,
    IHubContext<OrderHub> orderHub,
    IOptions<OrderOptions> orderOptions) : IOrderService
{
    private readonly decimal _taxRatePercent = orderOptions.Value.TaxRatePercent;

    public async Task<PagedResult<OrderListItemResponse>> GetListAsync(OrderListRequest request, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders.AsNoTracking().AsQueryable();

        // Gost vidi iskljucivo svoje narudzbe - filter po vlastitom Id-u nadjacava eventualni userId iz upita.
        var effectiveUserId = isStaff ? request.UserId : actorUserId;
        if (effectiveUserId.HasValue)
        {
            query = query.Where(x => x.UserId == effectiveUserId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= request.DateFrom);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= request.DateTo);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Number.Contains(request.Search));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "number" => request.SortDesc ? query.OrderByDescending(x => x.Number) : query.OrderBy(x => x.Number),
            "total" => request.SortDesc ? query.OrderByDescending(x => x.Total) : query.OrderBy(x => x.Total),
            "status" => request.SortDesc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => query.OrderByDescending(x => x.CreatedAt),
        };

        var projected = query.Select(x => new OrderListItemResponse(
            x.Id, x.Number, x.Type, x.Status,
            x.UserId, x.User.FirstName + " " + x.User.LastName,
            x.DiningTableId, x.DiningTable == null ? null : x.DiningTable.Number,
            x.Total, x.CreatedAt, x.Items.Count));

        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<OrderResponse> GetByIdAsync(int id, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var order = await LoadDetailedAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Narudžba sa Id {id} ne postoji.");

        if (!isStaff && order.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete pregledati tuđu narudžbu.");
        }

        return ToResponse(order);
    }

    public async Task<OrderQuoteResponse> QuoteAsync(PlaceOrderRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var calc = await BuildCalculationAsync(request, actorUserId, cancellationToken);

        return new OrderQuoteResponse(
            calc.Subtotal, calc.Discount, calc.Tax, calc.Total, _taxRatePercent, calc.Promotion?.Code,
            calc.Items.Select(x => new OrderQuoteItemResponse(x.MenuItem.Id, x.MenuItem.Name, x.Quantity, x.MenuItem.Price, x.MenuItem.Price * x.Quantity)).ToList());
    }

    public async Task<OrderResponse> CreateAsync(PlaceOrderRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var calc = await BuildCalculationAsync(request, actorUserId, cancellationToken);

        int? diningTableId = null;
        if (request.Type == OrderType.DineIn)
        {
            var table = await dbContext.DiningTables.FirstOrDefaultAsync(x => x.QrCode == request.TableCode && x.IsActive, cancellationToken)
                ?? throw new BusinessException($"Sto sa QR kodom '{request.TableCode}' ne postoji ili nije aktivan.");
            diningTableId = table.Id;
        }

        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Number = await GenerateOrderNumberAsync(cancellationToken),
            UserId = actorUserId,
            DiningTableId = diningTableId,
            Type = request.Type,
            Status = OrderStatus.Pending,
            Note = request.Note,
            Subtotal = calc.Subtotal,
            Tax = calc.Tax,
            Discount = calc.Discount,
            Total = calc.Total,
            PromotionId = calc.Promotion?.Id,
        };

        foreach (var item in calc.Items)
        {
            order.Items.Add(new OrderItem
            {
                MenuItemId = item.MenuItem.Id,
                Quantity = item.Quantity,
                UnitPrice = item.MenuItem.Price,
                Note = item.Note,
            });
        }

        if (calc.Promotion is not null)
        {
            calc.Promotion.UsedCount += 1;
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(order.Id, actorUserId, isStaff: true, cancellationToken);
    }

    public async Task<OrderResponse> TransitionAsync(int id, OrderTransitionRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.Include(x => x.User).Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Narudžba sa Id {id} ne postoji.");

        var oldStatusLabel = OrderStateMachine.GetStatusLabel(order.Status);
        stateMachine.Apply(order, request.Status, actorUserId, actorRoles, request.CancelReason);
        var newStatusLabel = OrderStateMachine.GetStatusLabel(order.Status);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (order.DiningTableId.HasValue)
        {
            await diningTableStatusService.RecalculateAsync(order.DiningTableId.Value, cancellationToken);
        }

        // Objavljivanje ide tek nakon uspjesnog SaveChangesAsync, nikad unutar transakcije.
        await eventPublisher.PublishAsync(
            new OrderStatusChangedEvent(
                Guid.NewGuid(), DateTime.UtcNow,
                order.Id, order.Number, order.UserId, order.User.Email ?? string.Empty, $"{order.User.FirstName} {order.User.LastName}",
                oldStatusLabel, newStatusLabel, order.CancelReason),
            EventRoutingKeys.OrderStatusChanged,
            cancellationToken);

        // API vec zna sve o prelazu u ovom trenutku (nije potreban RabbitMQ round-trip) - direktan push.
        var orderStatusMessage = new OrderStatusChangedMessage(order.Id, order.Number, order.UserId, oldStatusLabel, newStatusLabel);
        await orderHub.Clients.Group($"user:{order.UserId}").SendAsync("orderStatusChanged", orderStatusMessage, cancellationToken);
        await orderHub.Clients.Group("staff").SendAsync("orderStatusChanged", orderStatusMessage, cancellationToken);
        if (order.Status is OrderStatus.Confirmed or OrderStatus.Ready)
        {
            await orderHub.Clients.Group("kitchen").SendAsync("orderStatusChanged", orderStatusMessage, cancellationToken);
        }

        return await GetByIdAsync(id, actorUserId, isStaff: true, cancellationToken);
    }

    private sealed record CalcItem(MenuItem MenuItem, int Quantity, string? Note);

    private sealed record Calculation(List<CalcItem> Items, decimal Subtotal, decimal Discount, decimal Tax, decimal Total, Promotion? Promotion);

    private async Task<Calculation> BuildCalculationAsync(PlaceOrderRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        var menuItemIds = request.Items.Select(x => x.MenuItemId).Distinct().ToList();
        var menuItems = await dbContext.MenuItems
            .Where(x => menuItemIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var missingIds = menuItemIds.Except(menuItems.Select(x => x.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Items"] = [$"Jelo(a) sa Id {string.Join(", ", missingIds)} ne postoji(e)."],
            });
        }

        var unavailable = menuItems.Where(x => !x.IsAvailable).Select(x => x.Name).ToList();
        if (unavailable.Count > 0)
        {
            throw new BusinessException($"Sljedeća jela trenutno nisu dostupna: {string.Join(", ", unavailable)}.");
        }

        var menuItemsById = menuItems.ToDictionary(x => x.Id);
        var items = request.Items
            .Select(line => new CalcItem(menuItemsById[line.MenuItemId], line.Quantity, line.Note))
            .ToList();

        var subtotal = items.Sum(x => x.MenuItem.Price * x.Quantity);

        Promotion? promotion = null;
        var discount = 0m;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            promotion = await dbContext.Promotions.FirstOrDefaultAsync(x => x.Code == request.PromoCode, cancellationToken)
                ?? throw new BusinessException($"Promo kod '{request.PromoCode}' ne postoji.");

            if (!promotion.IsActive)
            {
                throw new BusinessException($"Promo kod '{promotion.Code}' nije aktivan.");
            }

            var now = DateTime.UtcNow;
            if (now < promotion.ValidFrom || now > promotion.ValidTo)
            {
                throw new BusinessException(
                    $"Promo kod '{promotion.Code}' nije važeći u ovom trenutku (važi od {promotion.ValidFrom:dd.MM.yyyy.} do {promotion.ValidTo:dd.MM.yyyy.}).");
            }

            if (promotion.MaxUses.HasValue && promotion.UsedCount >= promotion.MaxUses)
            {
                throw new BusinessException($"Promo kod '{promotion.Code}' je iskorišten maksimalan broj puta.");
            }

            if (promotion.MinOrderAmount.HasValue && subtotal < promotion.MinOrderAmount)
            {
                throw new BusinessException($"Minimalan iznos narudžbe za promo kod '{promotion.Code}' je {promotion.MinOrderAmount:0.00} KM.");
            }

            if (promotion.Scope == PromotionScope.FirstOrder)
            {
                var hasOrderedBefore = await dbContext.Orders
                    .AnyAsync(x => x.UserId == actorUserId && x.Status != OrderStatus.Cancelled, cancellationToken);
                if (hasOrderedBefore)
                {
                    throw new BusinessException($"Promo kod '{promotion.Code}' važi samo za prvu narudžbu.");
                }
            }

            var discountBase = promotion.Scope == PromotionScope.Category
                ? items.Where(x => x.MenuItem.MenuCategoryId == promotion.MenuCategoryId).Sum(x => x.MenuItem.Price * x.Quantity)
                : subtotal;

            discount = promotion.DiscountType == DiscountType.Percentage
                ? Math.Round(discountBase * promotion.Value / 100, 2)
                : Math.Min(promotion.Value, discountBase);
        }

        var tax = Math.Round((subtotal - discount) * _taxRatePercent / 100, 2);
        var total = subtotal - discount + tax;

        return new Calculation(items, subtotal, discount, tax, total, promotion);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"ORD-{DateTime.UtcNow:yyyyMMdd}-";
        var todayCount = await dbContext.Orders.IgnoreQueryFilters().CountAsync(x => x.Number.StartsWith(prefix), cancellationToken);
        return $"{prefix}{todayCount + 1:D4}";
    }

    private Task<Order?> LoadDetailedAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Orders.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.MenuItem)
            .Include(x => x.User)
            .Include(x => x.DiningTable)
            .Include(x => x.Waiter)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static OrderResponse ToResponse(Order order) => new(
        order.Id, order.Number, order.Type, order.Status,
        order.UserId, order.User.FirstName + " " + order.User.LastName,
        order.DiningTableId, order.DiningTable?.Number,
        order.WaiterId, order.Waiter == null ? null : order.Waiter.FirstName + " " + order.Waiter.LastName,
        order.Note,
        order.Subtotal, order.Discount, order.Tax, order.Total,
        order.PromotionId, order.Promotion?.Code,
        order.CreatedAt,
        order.ConfirmedAt, order.PreparationStartedAt, order.ReadyAt, order.DeliveredAt, order.CompletedAt, order.CancelledAt, order.CancelReason,
        order.Items.Select(i => new OrderItemResponse(i.Id, i.MenuItemId, i.MenuItem.Name, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity, i.Note)).ToList());
}
