namespace Sofra.API.Hubs.Messages;

/// <summary>Payload za OrderHub "reservationCreated".</summary>
public record ReservationCreatedMessage(int ReservationId, int UserId, string UserName, DateTime ReservationAt, int Guests, int ZoneId, string ZoneName);
