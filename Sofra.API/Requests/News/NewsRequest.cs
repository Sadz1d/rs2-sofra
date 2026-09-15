namespace Sofra.API.Requests.News;

public class NewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PublishAt { get; set; }
    public bool IsPublished { get; set; }
    public bool SendPush { get; set; }
}
