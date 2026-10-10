namespace SIGENRD.Core.Application.Wrappers
{
    public class PagedDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }
}
