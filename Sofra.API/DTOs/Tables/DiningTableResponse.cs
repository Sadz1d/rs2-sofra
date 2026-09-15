using Sofra.API.Enums;

namespace Sofra.API.DTOs.Tables;

public record DiningTableResponse(
    int Id, int Number, int Capacity,
    int ZoneId, string ZoneName,
    int TableTypeId, string TableTypeName,
    TableStatus Status,
    int? WaiterId, string? WaiterName,
    string QrCode, bool IsActive);
