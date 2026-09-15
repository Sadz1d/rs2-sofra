namespace Sofra.API.Hubs.Messages;

/// <summary>Payload za OrderHub "orderStatusChanged".</summary>
public record OrderStatusChangedMessage(int OrderId, string OrderNumber, int UserId, string OldStatus, string NewStatus);
