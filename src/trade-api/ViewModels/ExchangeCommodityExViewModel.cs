using System;
using System.Collections.Generic;
using trade_contracts;
using trade_model;

namespace trade_api.ViewModels
{
    public class ExchangeCommodityExViewModel
    {
        public Guid? CanonicalId { get; set; }

        public string NativeSymbol { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string NativeName { get; set; }
        public string ContractAddress { get; set; }

        public bool? CanDeposit { get; set; }
        public bool? CanWithdraw { get; set; }
        public decimal? WithdrawalFee { get; set; }

        public string Exchange { get; set; }
        public string DepositAddress { get; set; }
        public string DepositMemo { get; set; }
        public List<string> BaseSymbols { get; set; }

        public static ExchangeCommodityExViewModel FromModel(
            ExchangeCommodityContract model,
            string exchange,
            DepositAddressContract depositAddress,
            List<string> baseSymbols)
        {
            if (model == null) { return null; }
            return new ExchangeCommodityExViewModel
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
                DepositAddress = depositAddress?.DepositAddress,
                DepositMemo = depositAddress?.DepositMemo,
                BaseSymbols = baseSymbols
            };
        }
    }
}