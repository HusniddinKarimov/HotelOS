namespace HotelOS.Application.Common;

/// <summary>A page of results plus the metadata a UI needs to paginate.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}

/// <summary>Base query parameters for list endpoints (pagination, search, sorting).</summary>
public abstract class PagedQueryBase
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    private int _page = 1;

    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
    public int PageSize { get => _pageSize; set => _pageSize = value is < 1 or > MaxPageSize ? 20 : value; }

    /// <summary>Free-text search term.</summary>
    public string? Search { get; set; }

    /// <summary>Field to sort by (handler decides the allowed set).</summary>
    public string? SortBy { get; set; }

    /// <summary>"asc" (default) or "desc".</summary>
    public string? SortDir { get; set; }

    public bool Descending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}
