using config_connection_string_lib;
using idex_model;
using mongo_lib;
using MongoDB.Driver;
using System.Linq;

namespace idex_data_lib
{
    public class IdexHistoryRepo : IIdexHistoryRepo
    {
        private const string DatabaseName = "idex";

        private readonly IGetConnectionString _getConnectionString;

        public IdexHistoryRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(IdexHistoryContainer container)
        {
            HistoryContext.Insert(container);
        }

        public IdexHistoryContainer Get()
        {
            return HistoryCollection.AsQueryable().OrderByDescending(item => item.Id).FirstOrDefault();
        }

        private IMongoDatabaseContext DbContext => new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName);

        private IMongoCollectionContext HistoryContext => new MongoCollectionContext(DbContext, "idex--trade-history");

        private IMongoCollection<IdexHistoryContainer> HistoryCollection => HistoryContext.GetCollection<IdexHistoryContainer>();
    }
}
