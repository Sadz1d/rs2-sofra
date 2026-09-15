using Sofra.API.Enums;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Menu;

public class MenuItemListRequest : LookupListRequest
{
    public int? CategoryId { get; set; }
    public bool? IsAvailable { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>Iskljucuje jela koja sadrze ovaj alergen.</summary>
    public int? AllergenId { get; set; }

    public int? DietaryTagId { get; set; }
    public ServingPeriod? ServingPeriod { get; set; }
}
