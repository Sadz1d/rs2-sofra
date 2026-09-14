namespace Sofra.API.Requests.Catalog;

public class PaymentMethodRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}
