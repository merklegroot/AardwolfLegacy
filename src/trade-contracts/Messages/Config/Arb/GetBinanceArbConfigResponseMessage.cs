using trade_contracts.Models.Arb;

namespace trade_contracts.Messages.Config.Arb
{
    public class GetBinanceArbConfigResponseMessage : ResponseMessage
    {
        public BinanceArbConfigContract Payload { get; set; }
    }
}
