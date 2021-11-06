namespace trade_strategy_lib
{
    public class StrategyAction
    {
        public StrategyActionEnum ActionType { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
