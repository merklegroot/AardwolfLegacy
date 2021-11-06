using config_connection_string_lib;
using idex_model;
using mongo_lib;
using System;
using System.Linq;

namespace idex_data_lib
{
    public class IdexOrderBookRepo : IIdexOrderBookRepo
    {
        private const string DatabaseName = "idex";

        private readonly IGetConnectionString _getConnectionString;
        public IdexOrderBookRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(IdexOrderBookContainer container)
        {
            if (container == null) { throw new ArgumentNullException(nameof(container)); }
            if (string.IsNullOrWhiteSpace(container.Symbol)) { throw new ArgumentNullException(nameof(container.Symbol)); }
            if (string.IsNullOrWhiteSpace(container.BaseSymbol)) { throw new ArgumentNullException(nameof(container.BaseSymbol)); }

            var symbol = container.Symbol.Trim().ToUpper();
            var baseSymbol = container.BaseSymbol.Trim().ToUpper();

            var collectionContext = GetCollectionContext(symbol, baseSymbol);
            collectionContext.Insert(container);
        }

        public IdexOrderBookContainer Get(string symbol, string baseSymbol)
        {
            return GetCollectionContext(symbol, baseSymbol)
                .Get<IdexOrderBookContainer>()
                .FirstOrDefault();
        }

        private IMongoCollectionContext GetCollectionContext(string symbol, string baseSymbol)
        {
            return new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, $"idex--order-book-container--{symbol}-{baseSymbol}");
        }
    }
}
