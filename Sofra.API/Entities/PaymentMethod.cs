namespace Sofra.API.Entities;

public class PaymentMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}
