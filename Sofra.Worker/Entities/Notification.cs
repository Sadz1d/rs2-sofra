using Sofra.Worker.Enums;

namespace Sofra.Worker.Entities;

/// <summary>Minimalna kopija Sofra.API.Entities.Notification - Worker samo upisuje redove, ne cita ih.</summary>
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
