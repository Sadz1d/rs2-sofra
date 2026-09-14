namespace Sofra.API.Requests.Catalog;

public class CityListRequest : LookupListRequest
{
    public int? CountryId { get; set; }
}
