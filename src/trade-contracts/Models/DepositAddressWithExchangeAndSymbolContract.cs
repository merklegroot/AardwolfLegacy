namespace trade_contracts.Models
{
    public class DepositAddressWithExchangeAndSymbolContract
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string DepositAddress { get; set; }
        public string DepositMemo { get; set; }
    }
}
