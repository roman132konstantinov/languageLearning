using Application.DTOs.Common;

namespace Application.Common.Pagination
{
    public static class PagedQueryNormalizer
    {
        public static void Normalize(PagedQueryDto query)
        {
            ArgumentNullException.ThrowIfNull(query);

            query.Page = query.Page < 1 ? PagingDefaults.DefaultPage : query.Page;

            query.PageSize = query.PageSize switch
            {
                < 1 => PagingDefaults.DefaultPageSize,
                > PagingDefaults.MaxPageSize => PagingDefaults.MaxPageSize,
                _ => query.PageSize
            };

            query.Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            query.SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? null : query.SortBy.Trim();
            query.SortOrder = IsDescending(query.SortOrder)
                ? PagingDefaults.Descending
                : PagingDefaults.Ascending;
        }

        public static bool IsDescending(string? sortOrder)
        {
            return string.Equals(sortOrder, PagingDefaults.Descending, StringComparison.OrdinalIgnoreCase);
        }
    }
}
