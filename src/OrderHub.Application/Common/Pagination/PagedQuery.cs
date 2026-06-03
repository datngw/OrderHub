namespace OrderHub.Application.Common.Pagination;

public abstract record PagedQueryFilter
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; } = "desc";
}
