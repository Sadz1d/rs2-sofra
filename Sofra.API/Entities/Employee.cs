namespace Sofra.API.Entities;

public class Employee
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Position { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
