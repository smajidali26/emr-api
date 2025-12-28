namespace EMR.Application.Common.DTOs;

/// <summary>
/// Generic DTO for paged results
/// </summary>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResultDto()
    {
    }

    public PagedResultDto(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedResultDto<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PagedResultDto<T>(items, totalCount, pageNumber, pageSize);
    }
}
