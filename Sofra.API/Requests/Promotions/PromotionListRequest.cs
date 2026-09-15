using Sofra.API.Enums;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Promotions;

public class PromotionListRequest : LookupListRequest
{
    public bool? IsActive { get; set; }
    public PromotionScope? Scope { get; set; }

    /// <summary>Ako je true, vraca samo promocije koje su trenutno stvarno iskoristive (aktivna, u periodu vazenja, ima jos upotreba).</summary>
    public bool? IsCurrentlyValid { get; set; }
}
