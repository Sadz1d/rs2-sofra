namespace Sofra.API.Requests.Payments;

public class RefundRequest
{
    /// <summary>Ako nije poslano, refundira se cijeli preostali plaćeni iznos.</summary>
    public decimal? Amount { get; set; }
}
