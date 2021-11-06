using bit_z_model;
using config_connection_string_lib;
using mongo_lib;
using MongoDB.Driver;
using System;
using System.Linq;
using trade_model;

namespace bitz_data_lib
{
    public class BitzFundsRepo : IBitzFundsRepo
    {
        private const string DatabaseName = "bitz";
        private readonly IGetConnectionString _getConnectionString;

        public BitzFundsRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(BitzFundsContainer container)
        {
            var collectionContext = GetFundsCollectionContext();
            collectionContext.Insert(container);
        }

        public BitzFundsContainer GetMostRecent()
        {
            var collectionContext = GetFundsCollectionContext();
            var container = collectionContext.GetCollection<BitzFundsContainer>()
                .AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            return container;
        }

        public BitzFund GetBitzFund(string symbol)
        {
            var container = GetMostRecent();
            if (container == null) { return null; }

            return container.Funds?.SingleOrDefault(item => string.Equals(symbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase));
        }

        public DepositAddress GetDepositAddress(string symbol)
        {
            return GetBitzFund(symbol)?.DepositAddress;
        }

        private MongoCollectionContext GetFundsCollectionContext()
        {
            return new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "bitz--funds");
        }
    }
}
