using bit_z_model;
using config_connection_string_lib;
using mongo_lib;

namespace bitz_data_lib
{
    public class BitzTradeHistoryRepo : IBitzTradeHistoryRepo
    {
        private const string DatabaseName = "bitz";
        private readonly IGetConnectionString _getConnectionString;

        public BitzTradeHistoryRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void InsertTradeHistory(BitzTradeHistoryContainer container)
        {
            var context = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "bitz--trade-history");
            context.Insert(container);
        }
    }
}
