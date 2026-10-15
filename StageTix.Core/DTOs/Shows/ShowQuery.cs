using System.ComponentModel.DataAnnotations;
using StageTix.Core.DTOs.Common;
using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Shows;

/// <summary>Search, filter, sort and paging options for the show list.</summary>
public class ShowQuery : PagedQuery
{
    /// <summary>Free text matched against the title and the artist.</summary>
    [StringLength(100)]
    public string? Search { get; set; }

    public ShowCategory? Category { get; set; }

    /// <summary>Admin only. Customers and guests always get OnSale shows.</summary>
    public ShowStatus? Status { get; set; }

    public ShowSortBy SortBy { get; set; } = ShowSortBy.Date;

    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}
