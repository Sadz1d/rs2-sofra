namespace Sofra.API.Hubs.Messages;

/// <summary>Payload za OrderHub "orderCreated".</summary>
public record OrderCreatedMessage(int OrderId, string OrderNumber, int? DiningTableNumber);
