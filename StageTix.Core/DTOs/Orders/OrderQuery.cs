using StageTix.Core.DTOs.Common;
using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Orders;

/// <summary>Filter and paging options for the order list. Customers always get only their own orders.</summary>
public class OrderQuery : PagedQuery
{
    public OrderStatus? Status { get; set; }

    public int? ShowId { get; set; }
}
