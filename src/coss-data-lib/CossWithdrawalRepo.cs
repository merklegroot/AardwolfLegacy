using config_connection_string_lib;
using coss_lib.Models;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace coss_lib
{
    public class CossWithdrawalRepo
    {
        private const string DatabaseName = "coss";
        private static TimeSpan Expiration = TimeSpan.FromHours(2);

        private readonly IGetConnectionString _getConnectionString;        

        public CossWithdrawalRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;;
        }

        private IMongoCollectionContext WithdrawalRequestContext => new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "coss-withdrawal-request");
        private IMongoCollection<CossWithdrawalEvent> _withdrawalRequestCollection => WithdrawalRequestContext.GetCollection<CossWithdrawalEvent>();

        public void CreateRequest(string symbol, decimal quantity, string address)
        {
            var req = new CossWithdrawalEvent
            {
                TimeStampUtc = DateTime.UtcNow,
                Symbol = symbol,
                Quantity = quantity,
                DestinationAddress = address,
                EventType = CossWithdrawalEvent.CreateRequestEventType
            };

            _withdrawalRequestCollection.InsertOne(req);
        }

        // ineffecient, no concurrency checks, not fault tolereant.
        // this is a design in-progress.
        public List<CossWithdrawalEvent> GetOpenRequests()
        {
            var all = _withdrawalRequestCollection.AsQueryable()
                .Where(item => true)
                .ToList();

            var requests = all.Where(item => item.EventType == CossWithdrawalEvent.CreateRequestEventType).ToList();
            var commits = all.Where(item => item.EventType == CossWithdrawalEvent.CommitRequestEventType).ToList();

            var openRequests = requests.Where(queryRequest => !commits.Any(queryCommit =>  queryCommit.RelatedEventId == queryRequest.Id))
                .ToList();

            return openRequests;
        }

        public void CommitWithdrawal(ObjectId requestId)
        {
            var item = new CossWithdrawalEvent
            {
                RelatedEventId = requestId,
                TimeStampUtc = DateTime.UtcNow,
                EventType = CossWithdrawalEvent.CommitRequestEventType
            };
        }
    }
}
