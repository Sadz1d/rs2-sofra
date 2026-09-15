namespace Sofra.API.Requests.Reviews;

public class ReviewRequest
{
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
