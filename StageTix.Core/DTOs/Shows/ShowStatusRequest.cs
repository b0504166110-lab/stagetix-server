using System.ComponentModel.DataAnnotations;
using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Shows;

/// <summary>Publish a draft (OnSale) or cancel a show (Cancelled).</summary>
public class ShowStatusRequest
{
    /// <example>OnSale</example>
    [EnumDataType(typeof(ShowStatus))]
    public required ShowStatus Status { get; set; }
}
