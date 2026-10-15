using System.ComponentModel.DataAnnotations;
using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Orders;

/// <summary>Admin only: cancel an order. The only supported target status is Cancelled.</summary>
public class OrderStatusRequest
{
    /// <example>Cancelled</example>
    [EnumDataType(typeof(OrderStatus))]
    public required OrderStatus Status { get; set; }
}
