namespace trade_contracts
{
    public class CossAgentConfigContract
    {
        public bool IsCossAutoTradingEnabled { get; set; }        
        public decimal EthThreshold { get; set; }
        public decimal TokenThreshold { get; set; }
    }
}
