using config_connection_string_lib;
using idex_model;
using mongo_lib;
using MongoDB.Driver;
using System;
using System.Linq;
using trade_model;

namespace idex_data_lib
{
    public class IdexHoldingsRepo : IIdexHoldingsRepo
    {
        private const string ExchangeName = "Idex";
        private const string DatabaseName = "idex";

        private readonly IGetConnectionString _getConnectionString;
        public IdexHoldingsRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(IdexHoldingContainer container)
        {
            HoldingsContext.Insert(container);
        }

        public HoldingInfo Get()
        {
            var match = HoldingsContext.GetCollection<IdexHoldingContainer>()
                .AsQueryable()
                .Where(item => item.Holdings != null && item.Holdings.Count > 0)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            if (match == null) { return new HoldingInfo(); }

            var info = new HoldingInfo
            {
                TimeStampUtc = match.TimeStampUtc,
                Holdings = match.Holdings.Select(item =>
                {
                    return new Holding
                    {
                        Asset = item.Symbol,
                        Total = (item.IdexBalance ?? 0) + (item.IdexOnOrders ?? 0),
                        InOrders = item.IdexOnOrders ?? 0,
                        Available = item.IdexBalance ?? 0
                    };
                }).ToList()
            };

            return info;
        }

        public Holding GetHoldingForSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(symbol); }

            return Get()
                ?.Holdings
                ?.SingleOrDefault(item => string.Equals((item.Asset ?? string.Empty).Trim(), symbol.Trim(), StringComparison.InvariantCultureIgnoreCase));
        }

        private IMongoCollectionContext HoldingsContext => new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "idex--holdings");
    }
}
