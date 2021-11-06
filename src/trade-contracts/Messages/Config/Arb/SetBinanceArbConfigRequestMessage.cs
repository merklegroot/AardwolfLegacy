using trade_contracts.Models.Arb;

namespace trade_contracts.Messages.Config.Arb
{
    public class SetBinanceArbConfigRequestMessage : RequestMessage
    {
        public BinanceArbConfigContract Payload { get; set; }
    }
}
