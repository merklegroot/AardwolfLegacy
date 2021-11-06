using config_connection_string_lib;
using coss_data_model;
using mongo_lib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace coss_data_lib
{
    public class CossOpenOrderRepo : ICossOpenOrderRepo
    {
        private const string DatabaseName = "coss";
        private readonly IGetConnectionString _getConnectionString;

        public CossOpenOrderRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(CossOpenOrdersForTradingPairContainer container)
        {
            GetContext(container.Symbol, container.BaseSymbol).Insert(container);
        }

        public List<OpenOrder> Get(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            var effectiveSymbol = symbol.Trim().ToUpper();
            var effectiveBaseSymbol = baseSymbol.Trim().ToUpper();

            var container = GetContext(symbol, baseSymbol)
                .GetCollection<CossOpenOrdersForTradingPairContainer>()
                .AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            if (container == null || container.OpenOrders == null || !container.OpenOrders.Any())
            {
                return new List<OpenOrder>();
            }

            return (container?.OpenOrders?.Select(item => new OpenOrder
            {
                Price = item.Price,
                Quantity = item.Quantity,
                OrderType = item.OrderType
            }) ?? new List<OpenOrder>()).ToList();
        }

        private IMongoCollectionContext GetContext(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            var effectiveSymbol = symbol.Trim().ToUpper();
            var effectiveBaseSymbol = baseSymbol.Trim().ToUpper();

            return new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, $"coss--open-orders--{effectiveSymbol}-{effectiveBaseSymbol}");
        }
    }
}
