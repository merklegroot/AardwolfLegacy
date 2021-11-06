namespace yobit_lib.Models
{
    public class YobitCoin
    {
        public string Symbol { get; set; }
        public string FullName { get; set; }
        public string Algo { get; set; }
        public decimal? DiffPow { get; set; }
        public decimal? DiffPos { get; set; }
    }
}
