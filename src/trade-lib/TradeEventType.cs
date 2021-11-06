using log_lib.Models;

namespace trade_lib
{
    public class TradeEventType : EventType
    {
        public TradeEventType() { }
        public TradeEventType(string eventType) : base(eventType) { }

        public static TradeEventType RaiseBid { get { return new TradeEventType("RaiseBid"); } }
        public static TradeEventType LowerAsk { get { return new TradeEventType("LowerAsk"); } }
        public static TradeEventType CancelOrder { get { return new TradeEventType("CancelOrder"); } }
        public static TradeEventType KeepOrder { get { return new TradeEventType("KeepOrder"); } }
        public static TradeEventType AgentStarted { get { return new TradeEventType("AgentStarted"); } }
        public static TradeEventType BeginCheckWallet { get { return new TradeEventType("BeginCheckWallet"); } }
        public static TradeEventType EndCheckWallet { get { return new TradeEventType("EndCheckWallet"); } }
        public static TradeEventType BeginRefreshExchangeHistory { get { return new TradeEventType("BeginRefreshExchangeHistory"); } }
        public static TradeEventType EndRefreshExchangeHistory { get { return new TradeEventType("EndRefreshExchangeHistory"); } }
        public static TradeEventType PlaceBid { get { return new TradeEventType("PlaceBid"); } }
        public static TradeEventType PlaceAsk { get { return new TradeEventType("PlaceAsk"); } }

        public static TradeEventType AboutToPlaceOrder { get { return new TradeEventType("AboutToPlaceOrder"); } }
    }
}
