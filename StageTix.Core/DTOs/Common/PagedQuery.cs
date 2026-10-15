using System.ComponentModel.DataAnnotations;

namespace StageTix.Core.DTOs.Common;

/// <summary>Paging parameters shared by every list endpoint.</summary>
public abstract class PagedQuery
{
    public const int MaxPageSize = 50;

    /// <summary>1-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Items per page (1–50).</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 10;
}
