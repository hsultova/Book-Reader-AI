namespace BookReaderApp.Models;

// Shared wrapper for paginated list results. Per the project conventions, all list
// endpoints page their output (default 20 per page) instead of returning unbounded sets.
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public const int DefaultPageSize = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int pageSize = DefaultPageSize) =>
        new(Array.Empty<T>(), 1, pageSize, 0);
}
