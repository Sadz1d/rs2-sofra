using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.Shared.Events;
using Sofra.Worker.Data;
using Sofra.Worker.Entities;
using Sofra.Worker.Enums;
using Sofra.Worker.Options;

namespace Sofra.Worker.Messaging.Consumers;

public class LowStockDetectedConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<LowStockDetectedConsumer> logger)
    : EventConsumerBase<LowStockDetectedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.low-stock-detected", EventRoutingKeys.LowStockDetected)
{
    // Nema e-mail sablon za ovaj dogadjaj (nije trazen) - samo interna notifikacija za sve Admin korisnike.
    protected override Task HandleAsync(LowStockDetectedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<WorkerDbContext>();

        foreach (var adminUserId in @event.AdminUserIds)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = adminUserId,
                Type = NotificationType.LowStock,
                Title = "Niske zalihe",
                Text = $"Stanje namirnice '{@event.InventoryItemName}' je {@event.Quantity} {@event.UnitAbbreviation} (minimum {@event.MinQuantity} {@event.UnitAbbreviation}).",
                ReferenceId = @event.InventoryItemId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            });
        }

        return Task.CompletedTask;
    }
}
