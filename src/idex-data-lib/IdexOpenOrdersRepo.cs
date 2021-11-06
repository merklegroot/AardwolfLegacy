using System.Linq;
using System.Collections.Generic;
using idex_model;
using mongo_lib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using trade_model;
using System;
using config_connection_string_lib;

namespace idex_data_lib
{
    public class IdexOpenOrdersRepo : IIdexOpenOrdersRepo
    {
        private const string DatabaseName = "idex";

        private readonly IGetConnectionString _getConnectionString;

        private IMongoCollectionContext Context => new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, $"idex--open-orders");
            
        public IdexOpenOrdersRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(IdexOpenOrdersContainer container)
        {
            Context.Insert(container);
        }

        public List<OpenOrderForTradingPair> Get()
        {
            var container = Context.GetCollection<IdexOpenOrdersContainer>()
                .AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();
            
            if (container == null || container.OpenOrders == null) { return new List<OpenOrderForTradingPair>(); }

            var tradeTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Buy", OrderType.Bid },
                { "Sell", OrderType.Ask },
            };

            return container.OpenOrders.Select(item =>
            {
                var marketPieces = (item.Market ?? string.Empty).Split('/').Select(queryPiece => (queryPiece ?? string.Empty).Trim()).ToList();
                string symbol = null;
                string baseSymbol = null;
                if (marketPieces != null && marketPieces.Count == 2)
                {
                    symbol = (marketPieces[0] ?? string.Empty).Trim();
                    baseSymbol = (marketPieces[1] ?? string.Empty).Trim();
                }

                return new OpenOrderForTradingPair
                {
                    Price = item.Price,
                    Quantity = item.Quantity,
                    OrderType = tradeTypeDictionary.ContainsKey(item.TradeType) ? tradeTypeDictionary[item.TradeType] : OrderType.Unknown,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol
                };
            }).ToList();
        }
    }
}
