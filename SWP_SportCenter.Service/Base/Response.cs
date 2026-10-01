namespace SWP_SportCenter.Service.Base;

public class Response
{
    public class PageResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }
}