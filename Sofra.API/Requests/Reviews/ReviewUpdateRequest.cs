namespace Sofra.API.Requests.Reviews;

public class ReviewUpdateRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
