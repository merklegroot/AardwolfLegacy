namespace integration_workflow_lib.Models
{
    //public class ArbitrageResult
    //{
    //    public decimal EthQuantity { get; set; }
    //    public decimal? EthPrice { get; set; }
    //    public decimal? EthNeeded
    //    {
    //        get { return EthPrice.HasValue ? EthQuantity * EthPrice.Value : (decimal?)null; }
    //    }

    //    public decimal BtcQuantity { get; set; }
    //    public decimal? BtcPrice { get; set; }
    //    public decimal? BtcNeeded
    //    {
    //        get { return BtcPrice.HasValue ? BtcQuantity * BtcPrice.Value : (decimal?)null; }
    //    }

    //    public decimal ExpectedUsdCost { get; set; }
    //    public decimal ExpectedUsdProfit { get; set; }

    //    public decimal? ExpectedPercentProfit
    //    {
    //        get
    //        {
    //            if (ExpectedUsdCost <= 0) { return null; }
    //            return ExpectedUsdProfit / ExpectedUsdCost;
    //        }
    //    }
    //    public decimal TotalQuantity { get { return EthQuantity + BtcQuantity; } }
    //}
}
