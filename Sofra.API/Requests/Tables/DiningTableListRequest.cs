using Sofra.API.Enums;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Tables;

public class DiningTableListRequest : LookupListRequest
{
    public int? ZoneId { get; set; }
    public int? TableTypeId { get; set; }
    public TableStatus? Status { get; set; }
    public int? MinCapacity { get; set; }
}
