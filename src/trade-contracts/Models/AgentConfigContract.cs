namespace trade_contracts
{
    public class AgentConfigContract
    {
        public bool IsAutoTradingEnabled { get; set; }
        public decimal EthThreshold { get; set; }
        public decimal TokenThreshold { get; set; }
    }
}