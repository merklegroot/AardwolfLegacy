using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using config_connection_string_lib;
using coss_data_model;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using trade_model;

namespace coss_data_lib
{
    public class CossHistoryRepo : ICossHistoryRepo
    {
        private const string DatabaseName = "coss";
        private readonly IGetConnectionString _getConnectionString;

        public CossHistoryRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(CossResponseContainer<CossExchangeHistoryResponse> container)
        {
            ExchangeHistoryCollectionContext.Insert(container);
        }

        public void Insert(CossResponseContainer<CossDepositAndWithdrawalHistoryResponse> container)
        {
            DepositAndWithdrawalHistoryResponseCollectionContext.Insert(container);
        }
        
        public List<HistoricalTrade> Get()
        {
            var exchangeTrades = Task.Run(() => GetExchangeTrades());
            var depositsAndWithdrawals = Task.Run(() => GetDepositsAndWithdrawals());

            var merged = new List<HistoricalTrade>();
            merged.AddRange(exchangeTrades.Result ?? new List<HistoricalTrade>());
            merged.AddRange(depositsAndWithdrawals.Result ?? new List<HistoricalTrade>());

            return merged;
        }

        private List<HistoricalTrade> GetDepositsAndWithdrawals()
        {
            var bsonCollection = DepositAndWithdrawalHistoryResponseCollectionContext.GetCollection<BsonDocument>();
            var mostRecent = bsonCollection.AsQueryable()
                .OrderByDescending(item => item["_id"]).FirstOrDefault();

            if (mostRecent == null) { return new List<HistoricalTrade>(); }            
            var itemType = mostRecent.Contains("Type") ? mostRecent["Type"].ToString() : null;
            if (!string.Equals(itemType, typeof(CossResponseContainer<CossDepositAndWithdrawalHistoryResponse>).FullName))
            {
                return new List<HistoricalTrade>();
            }

            var container = BsonSerializer.Deserialize<CossResponseContainer<CossDepositAndWithdrawalHistoryResponse>>(mostRecent);

            if (container?.Response?.payload?.items == null)
            {
                return new List<HistoricalTrade>();
            }

            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "withdrawal", TradeTypeEnum.Withdraw },
                { "deposit", TradeTypeEnum.Deposit }
            };

            var tradeStatusDictionary = new Dictionary<string, TradeStatusEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "request_PROCESSING_AUTOMATICALLY", TradeStatusEnum.ProcessingAutomatically },
                { "request_CANCELED", TradeStatusEnum.Canceled }
            };

            var trades = new List<HistoricalTrade>();
            foreach (var item in container?.Response?.payload?.items)
            {
                var trade = new HistoricalTrade
                {
                    TradeType = tradeTypeDictionary.ContainsKey(item.action_code)
                        ? tradeTypeDictionary[item.action_code]
                        : TradeTypeEnum.Unknown,
                    TradeStatus = tradeStatusDictionary.ContainsKey(item.type_code)
                        ? tradeStatusDictionary[item.type_code]
                        : TradeStatusEnum.Unknown,
                    WalletAddress = item.wallet_address,
                    TransactionHash = item.transaction_hash,
                    Quantity = item.amount ?? default(decimal),
                    TimeStampUtc = item.created_at ?? default(DateTime),
                    Symbol = item.currency_code
                };

                trades.Add(trade);
            }

            return trades;
        }

        private List<HistoricalTrade> GetExchangeTrades()
        {
            var bsonCollection = ExchangeHistoryCollectionContext.GetCollection<BsonDocument>();
            var mostRecent = bsonCollection.AsQueryable().OrderByDescending(item => item["_id"]).FirstOrDefault();

            if (mostRecent == null) { return new List<HistoricalTrade>(); }        
            var container = BsonSerializer.Deserialize<CossResponseContainer<CossExchangeHistoryResponse>>(mostRecent);

            if (container?.Response?.payload?.actions?.items == null)
            {
                return new List<HistoricalTrade>();
            }

            var result = container.Response.payload.actions?.items
                ?.Select(item =>
                {
                    var tradeTypeDitionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "B", TradeTypeEnum.Buy },
                        { "S", TradeTypeEnum.Sell }
                    };

                    var trade = new HistoricalTrade
                    {
                        TimeStampUtc = item.created_at,
                        Quantity = item.amount,
                        TradingPair = new TradingPair(item.from_code, item.to_code),
                        Symbol = item.from_code,
                        BaseSymbol = item.to_code,
                        Price = item.price,
                        FeeQuantity = item.transaction_fee_total,
                        TradeType = tradeTypeDitionary.ContainsKey(item.order_direction)
                            ? tradeTypeDitionary[item.order_direction]
                            : TradeTypeEnum.Unknown
                    };

                    return trade;
                })
                .OrderByDescending(item => item.TimeStampUtc)
                .ToList();

            return result;
        }

        private IMongoCollectionContext ExchangeHistoryCollectionContext
        {
            get { return new MongoCollectionContext(DbContext, "coss--get-exchange-history"); }
        }
        
        private IMongoCollectionContext DepositAndWithdrawalHistoryResponseCollectionContext
        {
            get { return new MongoCollectionContext(DbContext, "coss--get-deposit-and-withdrawal-history"); }
        }

        private IMongoDatabaseContext DbContext { get { return new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName); } }
    }
}
