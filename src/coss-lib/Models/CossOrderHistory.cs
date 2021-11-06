using System;
using System.Collections.Generic;
using trade_model;

namespace coss_model
{
    public class CossOrderHistory
    {
        public string FromCommodity { get; set; }
        public string ToCommodity { get; set; }

        public List<CossOrderHistoryElement> Orders { get; set; }

        public static CossOrderHistory FromModel(
            CossOrderHistoryQueryResponse model,
            TradingPair tradingPair
            )
        {
            var item = new CossOrderHistory
            {
                FromCommodity = tradingPair.Symbol,
                ToCommodity = tradingPair.BaseSymbol,
                Orders = new List<CossOrderHistoryElement>()
            };

            foreach(var modelElement in model)
            {
                var itemElement = new CossOrderHistoryElement
                {
                    Id = modelElement.Id,
                    ActionText = modelElement.Action,
                    Amount = modelElement.Amount,
                    Price = modelElement.Price,
                    Total = modelElement.Total,
                    CreatedAt = modelElement.CreatedAt
                };

                item.Orders.Add(itemElement);
            }

            return item;
        }
    }

    public enum CossOrderAction
    {
        Unknown,
        Buy,
        Sell
    }

    public class CossOrderHistoryElement
    {
        public Guid Id { get; set; }

        public string ActionText { get; set; }

        public CossOrderAction Action
        {
            get
            {
                var dictionary = new Dictionary<string, CossOrderAction>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "buy", CossOrderAction.Buy },
                    { "sell", CossOrderAction.Sell }
                };

                return !string.IsNullOrWhiteSpace(ActionText) && dictionary.ContainsKey(ActionText.Trim())
                    ? dictionary[ActionText]
                    : CossOrderAction.Unknown;
            }
        }

        public decimal Amount { get; set; }

        public decimal Price { get; set; }

        public decimal Total { get; set; }

        public long CreatedAt { get; set; }
    }
}
