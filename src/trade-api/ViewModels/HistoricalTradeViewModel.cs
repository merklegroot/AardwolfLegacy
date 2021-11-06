using System;
using trade_model;

namespace trade_api.ViewModels
{
    public class HistoricalTradeViewModel
    {
        public string NativeId { get; set; }
        public string TradingPair { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public DateTime? SuccessTimeStampUtc { get; set; }
        public decimal? Price { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? FeeQuantity { get; set; }
        public string FeeCommodity { get; set; }
        public string Fee
        {
            get
            {
                if (!FeeQuantity.HasValue) { return null; }

                return $"{FeeQuantity.Value.ToString()} {(FeeCommodity ?? string.Empty)}".Trim();
            }
        }
            
        public string TradeType { get; set; }

        public string TradeStatus { get; set; }

        public string Exchange { get; set; }

        public string DestinationExchange { get; set; }

        public string WalletAddress { get; set; }

        public string TransactionHash { get; set; }

        public static HistoricalTradeViewModel FromModel(HistoricalTrade trade, string exchange)
        {
            if (trade == null) { return null; }

            return new HistoricalTradeViewModel
            {
                NativeId = trade.NativeId,
                TradingPair = trade.TradingPair?.ToString(),
                Symbol = trade.Symbol,
                BaseSymbol = trade.BaseSymbol,
                TimeStampUtc = trade.TimeStampUtc,
                SuccessTimeStampUtc = trade.SuccessTimeStampUtc,
                Price = trade.Price,
                Quantity = trade.Quantity,
                FeeQuantity = trade.FeeQuantity,
                FeeCommodity = trade.FeeCommodity,
                TradeType = trade.TradeType.ToString(),
                TradeStatus = trade.TradeStatus != TradeStatusEnum.Unknown
                    ? trade.TradeStatus.ToString()
                    : null,
                Exchange = exchange,
                WalletAddress = trade.WalletAddress,
                DestinationExchange = trade.DestinationExchange,
                TransactionHash = trade.TransactionHash
            };
        }
    }
}