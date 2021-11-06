namespace idex_model
{
    public class IdexHolding
    {
        public string Symbol { get; set; }
        public string CommodityName { get; set; }
        public decimal? MewBalance { get; set; }
        public decimal? IdexBalance { get; set; }
        public decimal? IdexOnOrders { get; set; }
        public decimal? IdexUsdValue { get; set; }
    }
}
