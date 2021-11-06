namespace trade_contracts.Messages.Exchange
{
    public class GetOrderBookResponseMessage : ResponseMessage
    {
        public OrderBookContract OrderBook { get; set; }
    }
}
