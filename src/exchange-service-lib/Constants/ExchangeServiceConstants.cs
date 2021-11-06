using trade_constants;

namespace exchange_service_lib.Constants
{
    public static class ExchangeServiceConstants
    {
        public const int Version = 3;
        public const string Queue = TradeRabbitConstants.Queues.ExchangeServiceQueue;
    }
}
