using config_client_lib;
using coss_ws_lib.Models;
using mongo_lib;
using System;

namespace coss_ws_lib
{
    public interface ICossWsWorkflow
    {
        void OnMessageReceived(DateTime timeStampUtc, string contract, string contents);
    }

    public class CossWsWorkflow : ICossWsWorkflow
    {
        private readonly IConfigClient _configClient;

        public CossWsWorkflow(IConfigClient configClient)
        {
            _configClient = configClient;
        }

        public void OnMessageReceived(
            DateTime timeStampUtc,
            string contract,
            string contents)
        {
            var model = new CossSockModel
            {
                TimeStampUtc = timeStampUtc,
                MessageContents = contents
            };

            MessageCollectionContext.Insert(model);
        }

        private IMongoCollectionContext MessageCollectionContext
        {
            get { return new MongoCollectionContext(DbContext, "coss-ws-message"); }
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), "coss-ws"); }
        }
    }
}
