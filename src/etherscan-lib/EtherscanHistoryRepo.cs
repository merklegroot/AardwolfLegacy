using config_connection_string_lib;
using etherscan_lib.Models;
using mongo_lib;
using MongoDB.Driver;
using parse_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace etherscan_lib
{
    public class EtherscanHistoryRepo : IEtherscanHistoryRepo
    {
        private const string DefaultDatabaseName = "etherscan";
        private readonly string _databaseName;

        private readonly IGetConnectionString _getConnectionString;

        public EtherscanHistoryRepo(IGetConnectionString connectionStringGetter) : this(connectionStringGetter, null)
        {
        }

        public EtherscanHistoryRepo(IGetConnectionString connectionStringGetter, string databaseName = null)
        {
            _databaseName = !string.IsNullOrWhiteSpace(databaseName) ? databaseName.Trim() : DefaultDatabaseName;
            _getConnectionString = connectionStringGetter;
        }

        private IMongoCollectionContext HistoryCollectionContext
        {
            get { return new MongoCollectionContext(_getConnectionString.GetConnectionString(), _databaseName, "etherscan--transaction-history"); }
        }

        private IMongoCollectionContext TransactionCollectionContext
        {
            get { return new MongoCollectionContext(_getConnectionString.GetConnectionString(), _databaseName, "etherscan--transaction"); }
        }

        public void Insert(EtherscanTransactionHistoryContainer container)
        {
            HistoryCollectionContext.Insert(container);
        }

        public EtherscanTransactionHistoryContainer GetNative()
        {
            return HistoryCollectionContext.GetCollection<EtherscanTransactionHistoryContainer>()
                .AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();
        }

        public List<HistoricalTrade> Get()
        {
            var historyContainer = GetNative();

            var hashes = GetTransactionHashes() ?? new List<string>();
            var transactions = hashes.Select(hash => GetTransaction(hash))
                .Where(item => item != null)
                .ToList();

            var history = transactions.Select(item => EtherscanTransactionToHistoricalTrade(item, historyContainer))
                .Where(item => item != null)
                .OrderByDescending(item => item.TimeStampUtc)
                .ToList();

            return history;
        }

        private HistoricalTrade EtherscanTransactionToHistoricalTrade(
            EtherscanTransactionContainer transactionContainer,
            EtherscanTransactionHistoryContainer historyContainer)
        {
            if (transactionContainer == null || transactionContainer.Data == null || !transactionContainer.Data.Any()) { return null; }

            var trade = new HistoricalTrade();

            var hash = 
                transactionContainer.Data
                    .FirstOrDefault(kvp => string.Equals(kvp.Key, "TxHash:", StringComparison.InvariantCultureIgnoreCase))
                    .Value;

            var matchingRow = 
                historyContainer.Rows?.FirstOrDefault(row =>
                row.Count > 0
                && string.Equals(row[0], hash, StringComparison.InvariantCultureIgnoreCase));

            if (matchingRow != null)
            {
                if (matchingRow.Count >= 5)
                {
                    var directionText = (matchingRow[4] ?? string.Empty).Trim();
                    var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "OUT", TradeTypeEnum.Withdraw },
                        { "IN", TradeTypeEnum.Deposit }
                    };

                    if (tradeTypeDictionary.ContainsKey(directionText))
                    {
                        trade.TradeType = tradeTypeDictionary[directionText];
                    }
                }
            }

            // TimeStamp:
            var timeStampPair = transactionContainer.Data.FirstOrDefault(item => string.Equals(item.Key, "TimeStamp:", StringComparison.InvariantCultureIgnoreCase));
            var timeStampRootText = (GetBetweenParentheses(timeStampPair.Value)
                ?? string.Empty)
                .Replace("+UTC", string.Empty);

            if (DateTime.TryParse(timeStampRootText, out DateTime timeStampRoot))
            {
                var timeStampPieces = timeStampPair.Value.Trim().Split(' ')
                    .Where(queryPiece => !string.IsNullOrWhiteSpace(queryPiece))
                    .Select(queryPiece => queryPiece.Trim())
                    .ToList();

                TimeSpan ago = TimeSpan.Zero;
                decimal? timeNum = null;
                for (var i = 0; i < timeStampPieces.Count; i++)
                {
                    var piece = (timeStampPieces[i] ?? string.Empty).Trim();                    

                    if (i % 2 == 0)
                    {
                        timeNum = ParseUtil.DecimalTryParse(piece);
                        if (!timeNum.HasValue) { break; }
                    }
                    else
                    {
                        if (string.Equals(piece, "Day", StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(piece, "Days", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ago = ago.Add(TimeSpan.FromDays((double)timeNum.Value));
                        }
                        else if (string.Equals(piece, "Hr", StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(piece, "Hrs", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ago = ago.Add(TimeSpan.FromHours((double)timeNum.Value));
                        }
                    }
                }

                trade.TimeStampUtc = timeStampRoot.Add(-ago);
            }

            // Actual Tx Cost/Fee:
            var feePair = transactionContainer.Data.FirstOrDefault(item => string.Equals(item.Key, "Actual Tx Cost/Fee:", StringComparison.InvariantCultureIgnoreCase));
            if (!string.IsNullOrWhiteSpace(feePair.Value))
            {
                var feePieces = feePair.Value.Trim().Split(' ').ToList();
                if (feePieces.Count > 0)
                {
                    var feeSegment = feePieces[0];
                    var fee = ParseUtil.DecimalTryParse(feeSegment);
                    trade.FeeQuantity = fee ?? 0;
                    if (fee.HasValue)
                    {
                        trade.FeeCommodity = "ETH";
                    }
                }
            }

            trade.NativeId = hash;

            // trade.Quantity
            var quantityPair = transactionContainer.Data.FirstOrDefault(item => string.Equals(item.Key, "Value:", StringComparison.InvariantCultureIgnoreCase));
            if (!string.IsNullOrWhiteSpace(quantityPair.Value))
            {
                // 1.05 Ether ($235.04)
                var pieces = quantityPair.Value.Trim().Split(' ');


                Console.WriteLine("here");
            }

            // k
            // trade.Fee = ParseUtil.DecimalTryParse()

            return trade;
        }

        private string GetBetweenParentheses(string text)
        {
            if (text== null) { return null; }
            var openPos = text.IndexOf("(");
            if (openPos < 0) { return null; }
            var closePos = text.IndexOf(")", openPos + 1);
            if (closePos < 0) { return null; }

            return text.Substring(openPos + 1, closePos - openPos - 1);
        }

        public List<string> GetTransactionHashes()
        {
            var native = GetNative();

            return native?.Rows?.Select(item => item != null && item.Count == 8 ? item[0] : null)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();
        }

        public void InsertTransaction(EtherscanTransactionContainer container)
        {
            TransactionCollectionContext.Insert(container);
        }

        public EtherscanTransactionContainer GetTransaction(string transactionHash)
        {
            return TransactionCollectionContext.GetCollection<EtherscanTransactionContainer>()
                .AsQueryable()                
                .Where(item => item.TransactionHash == transactionHash)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();
        }
    }
}
