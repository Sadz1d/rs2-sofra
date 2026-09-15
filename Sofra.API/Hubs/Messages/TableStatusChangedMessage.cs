namespace Sofra.API.Hubs.Messages;

/// <summary>Payload za OrderHub "tableStatusChanged".</summary>
public record TableStatusChangedMessage(int TableId, int TableNumber, string Status);
