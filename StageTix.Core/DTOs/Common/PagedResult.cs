namespace StageTix.Core.DTOs.Common;

/// <summary>One page of results plus the information the client needs to render paging controls.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    /// <summary>Number of items across all pages.</summary>
    public int TotalCount { get; set; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
