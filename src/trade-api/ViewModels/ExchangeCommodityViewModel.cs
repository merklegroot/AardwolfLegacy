using System;
using System.Collections.Generic;
using trade_contracts;
using trade_model;

namespace trade_api.ViewModels
{
    public class ExchangeCommodityViewModel
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

        public bool IsEth { get; set; }
        public bool? IsEthToken { get; set; }
        public decimal? LotSize { get; set; }

        public Dictionary<string, string> CustomValues { get; set; }

        public static ExchangeCommodityViewModel FromModel(ExchangeCommodityContract model)
        {
            if (model == null) { return null; }

            var item = new ExchangeCommodityViewModel
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
                CustomValues = model.CustomValues,
                LotSize = model.LotSize
            };

            return item;
        }

        public static ExchangeCommodityViewModel FromModel(CommodityForExchange model)
        {
            if (model == null) { return null; }

            var item = new ExchangeCommodityViewModel
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
                CustomValues = model.CustomValues,
                LotSize = model.LotSize
            };

            return item;
        }
    }

}