namespace trade_contracts
{
    public class ArbitrageResultContract
    {
        public class SymbolAndQuantityContract
        {
            public string Symbol { get; set; }
            public decimal Quantity { get; set; }

            public SymbolAndQuantityContract() { }
            public SymbolAndQuantityContract(string symbol, decimal quantity)
            {
                Symbol = symbol;
                Quantity = quantity;
            }
        }

        public decimal EthQuantity { get; set; }
        public decimal? EthPrice { get; set; }
        public decimal? EthNeeded { get; set; }
        public decimal BtcQuantity { get; set; }
        public decimal? BtcPrice { get; set; }
        public decimal? BtcNeeded { get; set; }
        public decimal ExpectedUsdCost { get; set; }
        public decimal ExpectedUsdProfit { get; set; }
        public decimal? ExpectedProfitRatio { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}
