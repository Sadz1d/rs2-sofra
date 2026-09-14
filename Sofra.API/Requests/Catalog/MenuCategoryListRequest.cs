namespace Sofra.API.Requests.Catalog;

public class MenuCategoryListRequest : LookupListRequest
{
    public bool? IsActive { get; set; }
}
