using Sofra.API.Enums;

namespace Sofra.API.Requests.Tables;

public class DiningTableRequest
{
    public int Number { get; set; }
    public int Capacity { get; set; }
    public int ZoneId { get; set; }
    public int TableTypeId { get; set; }
    public int? WaiterId { get; set; }
    public string? QrCode { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Free;
}
