using config_connection_string_lib;
using coss_browser_service_lib.Models;
using coss_browser_workflow_lib.Models;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace coss_browser_service_lib.Repo
{
    public interface ICossCookieRepo
    {
        CossCookieContainer Get();
        void Set(CossCookieContainer container);
    }

    public class CossCookieRepo : ICossCookieRepo
    {
        private const string DatabaseName = "coss";

        private readonly IGetConnectionString _getConnectionString;

        public CossCookieRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public CossCookieContainer Get()
        {
            var entity = GetCookieContext().GetLast<CookieContainerEntity>();
            return entity != null
                ? new CossCookieContainer
                {
                    SessionToken = entity.SessionToken,
                    XsrfToken = entity.XsrfToken
                }
                : null;
        }

        public void Set(CossCookieContainer container)
        {
            var entity = new CookieContainerEntity
            {
                TimeStampUtc = DateTime.UtcNow,
                SessionToken = container.SessionToken,
                XsrfToken = container.XsrfToken
            };

            var context = GetCookieContext();
            context.Insert(entity);

            // var bsonCollection = context.GetCollection<BsonDocument>();
            // var filter = Builders<BsonDocument>.Filter.Lt("_id", entity.Id);
            // bsonCollection.DeleteMany(filter);
        }

        private MongoCollectionContext GetCookieContext() => 
            new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "coss--cookie");
    }
}
