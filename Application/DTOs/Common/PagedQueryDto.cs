using Application.Common.Pagination;

namespace Application.DTOs.Common
{
    public abstract class PagedQueryDto
    {
        public int Page { get; set; } = PagingDefaults.DefaultPage;
        public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = PagingDefaults.Ascending;
    }
}
