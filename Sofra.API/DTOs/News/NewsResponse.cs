namespace Sofra.API.DTOs.News;

public record NewsResponse(
    int Id, string Title, string Text, string ImageUrl,
    DateTime PublishAt, bool IsPublished, bool SendPush,
    int CreatedById, string CreatedByName);
