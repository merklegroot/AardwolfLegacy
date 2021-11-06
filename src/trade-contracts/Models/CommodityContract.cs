using System;

namespace trade_contracts
{
    public class CommodityContract
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public bool IsEth { get; set; }
        public bool? IsEthToken { get; set; }
        public string ContractId { get; set; }
        public int? Decimals { get; set; }
        public bool IsDominant { get; set; }
    }
}
