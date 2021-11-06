namespace coss_lib.Models
{
    public class CossResponseAndUrl<T>
    {
        public string Url { get; set; }
        public T Response { get; set; }
    }
}
