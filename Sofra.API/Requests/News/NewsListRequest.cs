using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.News;

public class NewsListRequest : LookupListRequest
{
    public bool? IsPublished { get; set; }
}
