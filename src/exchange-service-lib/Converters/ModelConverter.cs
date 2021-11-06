using System.Collections.Generic;
using trade_contracts;
using trade_model;

namespace exchange_service_lib.Extensions
{
    public static class ModelConverter
    {
        public static DetailedExchangeCommodityContract ToDetailedExchangeCommodityContract(
            CommodityForExchange model,
            string exchange,
            DepositAddress depositAddress,
            List<string> baseSymbols)
        {
            if (model == null) { return null; }
            return new DetailedExchangeCommodityContract
            {
                CanonicalId = model.CanonicalId,
                NativeSymbol = model.NativeSymbol,
                Symbol = model.Symbol,
                Name = model.Name,
                NativeName = model.NativeName,
                ContractAddress = model.ContractAddress,
                CanDeposit = model.CanDeposit,
                CanWithdraw = model.CanWithdraw,
                WithdrawalFee = model.WithdrawalFee,
                Exchange = exchange,
                DepositAddress = depositAddress?.Address,
                DepositMemo = depositAddress?.Memo,
                BaseSymbols = baseSymbols,
                LotSize = model.LotSize
            };
        }
    }
}
