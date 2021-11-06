using trade_model;

namespace balance_lib
{
    public interface IBalanceAggregator
    {
        HoldingInfoViewModel GetHoldingsForExchange(GetHoldingsForExchangeServiceModel serviceModel);
    }
}
