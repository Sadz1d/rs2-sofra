using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class DiningTable
{
    public int Id { get; set; }
    public int Number { get; set; }
    public int Capacity { get; set; }
    public int ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;
    public int TableTypeId { get; set; }
    public TableType TableType { get; set; } = null!;
    public TableStatus Status { get; set; }
    public int? WaiterId { get; set; }
    public ApplicationUser? Waiter { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
