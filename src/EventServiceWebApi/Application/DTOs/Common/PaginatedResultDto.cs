namespace EventServiceWebApi.Application.DTOs.Common;

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
public record PaginatedResultDto<T>
{
    /// <summary>
    /// Current page number.
    /// </summary>
    /// <example>1</example>
    public int Page { get; init; }

    /// <summary>
    /// Number of items requested per page.
    /// </summary>
    /// <example>10</example>
    public int PageSize { get; init; }

    /// <summary>
    /// Total count of items matching the filter (across all pages).
    /// </summary>
    /// <example>42</example>
    public int TotalItemsCount { get; init; }

    /// <summary>
    /// Collection of items on the current page.
    /// </summary>
    public IReadOnlyCollection<T> Items { get; init; } = [];

    public PaginatedResultDto(int page, int pageSize, int totalItemsCount, IReadOnlyCollection<T> items)
    {
        Page = page;
        PageSize = pageSize;
        TotalItemsCount = totalItemsCount;
        Items = items;
    }
}
