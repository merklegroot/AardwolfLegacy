using service_lib.Handlers;
using trade_contracts.Messages.Exchange;

namespace exchange_service_lib.Handlers
{
    public interface IGetOrderBookHandler : IRequestResponseHandler<GetOrderBookRequestMessage, GetOrderBookResponseMessage>
    {
    }

    public class GetOrderBookHandler
    {
    }
}
