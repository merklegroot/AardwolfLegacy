using MongoDB.Bson.Serialization.Attributes;
using parse_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace idex_model
{
    public class IdexHistoryItem : List<string>
    {
        [BsonIgnore]
        public string Id => GetItem(0);
        
        [BsonIgnore]
        public string Symbol => SymbolAndBaseSymbol.Symbol;

        [BsonIgnore]
        public string BaseSymbol => SymbolAndBaseSymbol.BaseSymbol;

        [BsonIgnore]
        public TradeTypeEnum TradeType
        {
            get
            {
                var operationText = GetItem(2);
                if (string.IsNullOrWhiteSpace(operationText)) { return TradeTypeEnum.Unknown; }

                var operationDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "Buy", TradeTypeEnum.Buy },
                    { "Sell", TradeTypeEnum.Sell }
                };

                var trimmedText = operationText.Trim();

                return operationDictionary.ContainsKey(trimmedText)
                    ? operationDictionary[trimmedText]
                    : TradeTypeEnum.Unknown;
            }
        }

        [BsonIgnore]
        public decimal? Price => ParseUtil.DecimalTryParse(GetItem(3));

        [BsonIgnore]
        public decimal? Quantity
        {
            get
            {
                var text = GetItem(4);
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                var pieces = text.Trim().Split(' ').ToList();
                if (pieces.Count != 2) { return null; }

                return ParseUtil.DecimalTryParse(pieces[0]);
            }
        }

        /// <summary>
        /// The container should have the time zone so that it can be converted to UTC.
        /// </summary>
        [BsonIgnore]
        public DateTime? TimeStampLocal => ParseUtil.DateTimeTryParse(GetItem(8));

        [BsonIgnore]
        public string Status => GetItem(9);

        [BsonIgnore]
        public decimal? FeeQuantity => FeeQuantityAndSymbol.Quantity;

        [BsonIgnore]
        public string FeeSymbol => FeeQuantityAndSymbol.Symbol;

        private (decimal? Quantity, string Symbol) FeeQuantityAndSymbol
        {
            get
            {
                var item = GetItem(5);
                if (string.IsNullOrWhiteSpace(item)) { return (null, null); }
                var pieces = item.Trim().Split(' ').ToList();
                if (pieces.Count != 2) { return (null, null); }
                var feeQuantityPiece = pieces[0];
                var feeSymbolPiece = pieces[1];

                var feeQuantity = ParseUtil.DecimalTryParse(pieces[0]);
                var feeSymbol = pieces[1] != null ? pieces[1].Trim() : null;

                return (feeQuantity, feeSymbol);
            }
        }


        //public string GasFeeText { get; set; }
        //public string Total { get; set; }
        //public string TimeStamp { get; set; }

        private string GetItem(int index) => Count >= index + 1 ? this[index] : null;
        private (string Symbol, string BaseSymbol) SymbolAndBaseSymbol
        {
            get
            {
                var tradingPairText = GetItem(1);
                if (string.IsNullOrWhiteSpace(tradingPairText)) { return (null, null); }
                var pieces = tradingPairText.Split('/').ToList();
                if (pieces.Count != 2) { return (null, null); }
                return (pieces[0], pieces[1]);
            }
        }
    }
}
