namespace KSS.Helper.Model
{
    public class Pagination
    {
        public int PageNumber { get; set; } = 1;
        private int _pageSize = Assistant.DefaultPageSize;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > Assistant.MaxPageSize ? Assistant.MaxPageSize : value;
        }
    }
}