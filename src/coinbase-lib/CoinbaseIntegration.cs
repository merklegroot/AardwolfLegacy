using coinbase_lib.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using cache_lib.Models;
using trade_model;
using coinbase_lib.res;
using config_client_lib;
using trade_node_integration;
using log_lib;
using Newtonsoft.Json;

namespace coinbase_lib
{
    public class CoinbaseIntegration : ICoinbaseIntegration
    {
        public string Name { get { return "Coinbase"; } }

        public Guid Id => new Guid("3D917AD2-982B-492D-911E-6DDC098F5851");

        // private const string FileName = @"C:\trade\coinbase\coinbase-history.txt";
        private const string HistoryTextFileName = @"C:\trade\data\coinbase\coinbase-history.txt";
        private const string TransactionsFileName = @"c:\trade\data\coinbase\coinbase-transactions.json";
        private const string AccountsFileName = @"c:\trade\data\coinbase\coinbase-accounts.json";

        private readonly CoinbaseMap _map;
        private readonly IConfigClient _configClient;

        private readonly ILogRepo _log;

        public CoinbaseIntegration(
            IConfigClient configClient,
            ITradeNodeUtil nodeUtil,
            ILogRepo log)
        {
            _configClient = configClient;
            _map = new CoinbaseMap();
            _log = log;
        }

        public List<CoinbaseAccountWithTranscations> GetJsonHistory()
        {
            var contents = File.ReadAllText(TransactionsFileName);
            var accountsWithTransactions = JsonConvert.DeserializeObject<List<CoinbaseAccountWithTranscations>>(contents);

            return accountsWithTransactions;
        }
        
        // since this is coming from a file for now, cachePolicy is ignored.
        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            return GetUserTradeHistoryV2(cachePolicy)
                ?.History;
        }

        private List<HistoricalTrade> ParseHistoricalTrades(string contents)
        {
            var coinbaseTrades = contents.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => ParseLine(line.Trim()))
                .ToList();

            var tradeTypes = coinbaseTrades.Select(item => item.TradeType).Distinct().ToList();

            var results = coinbaseTrades
                .Select(item =>
            {
                if (item == null) { throw new ApplicationException($"{nameof(item)} must not be null."); }

                var getTradeType = new Func<CoinbaseTradeType, TradeTypeEnum>(native =>
                {
                    if (native == CoinbaseTradeType.Buy) { return TradeTypeEnum.Buy; }
                    if (native == CoinbaseTradeType.Sell) { return TradeTypeEnum.Sell; }
                    if (native == CoinbaseTradeType.Withdraw) { return TradeTypeEnum.Withdraw; }

                    return TradeTypeEnum.Unknown;
                });

                var tradeType = getTradeType(item.TradeType);

                var symbol = (tradeType == TradeTypeEnum.Buy || tradeType == TradeTypeEnum.Sell)
                    ? item.Got?.Commodity
                    : tradeType == TradeTypeEnum.Withdraw 
                    ? item.Gave?.Commodity : null;

                var baseSymbol = (tradeType == TradeTypeEnum.Buy || tradeType == TradeTypeEnum.Sell)
                    ? item.Gave?.Commodity
                    : null;

                var quantity = (tradeType == TradeTypeEnum.Buy || tradeType == TradeTypeEnum.Sell)
                    ? item.Got?.Quantity
                    : tradeType == TradeTypeEnum.Withdraw
                    ? item.Gave?.Quantity : 0;

                var tradingPair = symbol != null && baseSymbol != null
                    ? ToTradingPair(symbol, baseSymbol)
                    : null;

                var historicalTrade = new HistoricalTrade
                {
                    TradingPair = tradingPair,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    Quantity = quantity ?? 0,
                    Price = item.Price?.Quantity ?? 0,
                    FeeQuantity = item.Fee?.Quantity ?? 0,
                    TimeStampUtc = item.TimeStampUtc,
                    TradeType = tradeType
                };

                return historicalTrade;
            }).ToList();

            return results;
        }

        public CoinbaseHistoricalTrade ParseLine(string line)
        {
            var linePieces = line.Split('\t').Where(piece => !string.IsNullOrWhiteSpace(piece)).Select(piece => piece.Trim()).ToList();
            var tradeType = ParseTradeType(linePieces[0]);

            if (tradeType == CoinbaseTradeType.Withdraw)
            {
                const int ExpectedPieceCount = 5;
                if (linePieces.Count() != ExpectedPieceCount)
                {
                    var errorText = new StringBuilder()
                        .AppendLine($"Expected {ExpectedPieceCount} pieces in line but found {linePieces.Count()}.")
                        .AppendLine("Line:")
                        .AppendLine(line)
                        .ToString();

                    throw new ApplicationException(errorText);
                }

                var quantity = ParseQuantity(linePieces[1]);
                var fee = ParseQuantity(linePieces[2]);
                var timeStamp = DateTime.Parse(linePieces[4].Trim());
                var status = ParseStatus(linePieces[3]);

                return new CoinbaseHistoricalTrade
                {
                    TradeType = tradeType,
                    Gave = quantity,
                    Fee = fee,
                    Got = null,
                    Price = null,
                    Status = status,
                    TimeStampUtc = timeStamp                    
                };
            }
            else if (tradeType == CoinbaseTradeType.Buy || tradeType == CoinbaseTradeType.Sell)
            {
                const int ExpectedPieceCount = 7;
                if (linePieces.Count() != ExpectedPieceCount)
                {
                    var errorText = new StringBuilder()
                        .AppendLine($"Expected {ExpectedPieceCount} pieces in line but found {linePieces.Count()}.")
                        .AppendLine("Line:")
                        .AppendLine(line)
                        .ToString();

                    throw new ApplicationException(errorText);
                }

                var gave = ParseQuantity(linePieces[1]);
                var got = ParseQuantity(linePieces[2]);
                var fee = ParseQuantity(linePieces[3]);
                var price = ParseQuantity(linePieces[4]);
                var status = ParseStatus(linePieces[5]);
                var timeStamp = DateTime.Parse(linePieces[6].Trim());

                return new CoinbaseHistoricalTrade
                {
                    TradeType = tradeType,
                    Gave = gave,
                    Got = got,
                    Fee = fee,
                    Price = price,
                    Status = status,
                    TimeStampUtc = timeStamp
                };
            }

            throw new ApplicationException($"Unexpected trade type \"{tradeType}\".");
        }

        private CoinbaseQuantityAndCommodity ParseQuantity(string text)
        {
            var pieces = text.Split(' ');
            var quantity = decimal.Parse(pieces[0].Trim().Replace(",", ""), NumberStyles.Float);
            var commodity = pieces[1].Trim();

            return new CoinbaseQuantityAndCommodity { Quantity = quantity, Commodity = commodity };
        }

        private CoinbaseTradeStatus ParseStatus(string text)
        {
            var dict = new Dictionary<string, CoinbaseTradeStatus>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "COMPLETED", CoinbaseTradeStatus.Completed },
                { "CANCELED", CoinbaseTradeStatus.Cancelled }
            };

            var effectiveText = (text ?? string.Empty).Trim();

            return dict.ContainsKey(effectiveText)
                ? dict[effectiveText]
                : CoinbaseTradeStatus.Unknown;
        }

        private CoinbaseTradeType ParseTradeType(string text)
        {
            var effectiveText = (text ?? string.Empty).Trim();

            var dict = new Dictionary<string, CoinbaseTradeType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Buy", CoinbaseTradeType.Buy },
                { "Sell", CoinbaseTradeType.Sell },
                { "Withdrawal", CoinbaseTradeType.Withdraw },
            };

            if (!dict.ContainsKey(effectiveText)) { throw new ApplicationException($"Unexpected trade type \"{effectiveText}\""); }

            return dict[effectiveText];
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var symbols = new List<string>
            {
                "ETH", "BTC", "USD", "LTC", "BCH"
            };

            symbols.Select(querySymbol =>
            {
                var canon = _map.GetCanon(querySymbol);

                return new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = canon?.Symbol ?? querySymbol,
                    NativeSymbol = querySymbol,
                    
                };
                
            });

            return new List<CommodityForExchange>
            {
                new CommodityForExchange
                {
                    Symbol = "BTC",
                    NativeSymbol = "BTC"
                },
                new CommodityForExchange
                {
                    Symbol = "ETH",
                    NativeSymbol = "ETH"
                },
                new CommodityForExchange
                {
                    Symbol = "USD",
                    NativeSymbol = "USD"
                },
                new CommodityForExchange
                {
                    Symbol = "LTC",
                    NativeSymbol = "LTC"
                },
                new CommodityForExchange
                {
                    Symbol = "BCH",
                    NativeSymbol = "BCH"
                }
            };
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            return new List<TradingPair>
            {
                new TradingPair
                {
                    Symbol = "ETH",
                    BaseSymbol = "BTC"
                },
                new TradingPair
                {
                    Symbol = "ETH",
                    BaseSymbol = "USD"
                },
                new TradingPair
                {
                    Symbol = "BTC",
                    BaseSymbol = "USD"
                },
                new TradingPair
                {
                    Symbol = "LTC",
                    BaseSymbol = "USD"
                },
                new TradingPair
                {
                    Symbol = "BCH",
                    BaseSymbol = "USD"
                }
            };
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public List<CoinbaseAccount> GetAccounts()
        {
            const string FileName = @"C:\trade\data\coinbase\coinbase-accounts.json";
            var contents = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<List<CoinbaseAccount>>(contents);
        }

        public class CoinbaseTransactionsAndAccountId
        {
            public string AccountId { get; set; }
            public List<CoinbaseTransaction> Transactions { get; set; }
        }

        public List<CoinbaseTransactionsAndAccountId> GetAccountsWithTransactions()
        {
            const string FileName = @"C:\trade\data\coinbase\coinbase-transactions.json";
            var contents = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<List<CoinbaseTransactionsAndAccountId>>(contents);
        }

        public List<CoinbaseTrade> GetBuys()
        {
            const string FileName = @"C:\trade\data\coinbase\coinbase-buys.json";
            var contents = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<List<CoinbaseTrade>>(contents);
        }

        public List<CoinbaseTrade> GetSells()
        {
            const string FileName = @"C:\trade\data\coinbase\coinbase-sells.json";
            var contents = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<List<CoinbaseTrade>>(contents);
        }

        public List<HistoricalTrade> GetHistoryFromJson()
        {
            var accounts = GetAccounts();
            var accountsWithTransactions = GetAccountsWithTransactions();
            var buys = GetBuys();
            var sells = GetSells();

            var historyItems = new List<HistoricalTrade>();
            foreach (var accountWithTransactions in accountsWithTransactions)
            { 
                foreach (var transaction in accountWithTransactions.Transactions)
                {
                    var accountId = accountWithTransactions.AccountId;
                    var coinbaseTransactionTypeDictionary = new Dictionary<string, CoinbaseTransactionTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "sell", CoinbaseTransactionTypeEnum.Sell },
                    { "buy", CoinbaseTransactionTypeEnum.Buy },
                    { "send", CoinbaseTransactionTypeEnum.Send },
                    { "fiat_withdrawal", CoinbaseTransactionTypeEnum.FiatWithdraw }
                };

                    var coinbaseTransactionType = coinbaseTransactionTypeDictionary.ContainsKey(transaction.Type)
                        ? coinbaseTransactionTypeDictionary[transaction.Type]
                        : CoinbaseTransactionTypeEnum.Unknown;

                    var walletAddress = "";
                    if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.FiatWithdraw)
                    {
                        walletAddress = transaction.Details?.PaymentMethodName;
                    }
                    else if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.Send)
                    {
                        walletAddress = transaction.To?.Address;
                    }

                    var tradeTypeDictionary = new Dictionary<CoinbaseTransactionTypeEnum, TradeTypeEnum>()
                {
                    { CoinbaseTransactionTypeEnum.Sell, TradeTypeEnum.Sell },
                    { CoinbaseTransactionTypeEnum.Buy, TradeTypeEnum.Buy },
                    { CoinbaseTransactionTypeEnum.Send, TradeTypeEnum.Withdraw },
                    { CoinbaseTransactionTypeEnum.FiatWithdraw, TradeTypeEnum.Withdraw }
                };

                    var tradeType = tradeTypeDictionary.ContainsKey(coinbaseTransactionType)
                        ? tradeTypeDictionary[coinbaseTransactionType]
                        : TradeTypeEnum.Unknown;

                    var symbol = transaction?.Amount?.Currency;
                    var baseSymbol = transaction?.NativeAmount?.Currency;
                    var tradingPair = !string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(baseSymbol)
                        ? new TradingPair(symbol, baseSymbol)
                        : null;

                    decimal? price = null;
                    if (transaction?.Amount?.Amount != null && transaction?.NativeAmount?.Amount != null)
                    {
                        if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.Buy)
                        {
                            price = transaction.NativeAmount.Amount / transaction.Amount.Amount;
                        }
                        else if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.Buy)
                        {
                            price = transaction.Amount.Amount / transaction.NativeAmount.Amount;
                        }
                    }

                    var hash = transaction?.Network?.Hash;

                    var feeQuantity = transaction.Network?.TransactionFee?.Amount;
                    var feeCommodity = transaction.Network?.TransactionFee?.Currency;

                    var historyItem = new HistoricalTrade
                    {
                        NativeId = transaction.Id.ToString(),
                        Quantity = Math.Abs(transaction.Amount?.Amount ?? default(decimal)),
                        Symbol = symbol,
                        BaseSymbol = baseSymbol,
                        TradingPair = tradingPair,
                        TimeStampUtc = transaction.CreatedAt ?? default(DateTime),
                        TradeType = tradeType,
                        WalletAddress = walletAddress,
                        Price = price ?? default(decimal),
                        TransactionHash = hash,
                        FeeQuantity = feeQuantity ?? default(decimal),
                        FeeCommodity = feeCommodity
                    };

                    var matchingBuy = buys.SingleOrDefault(queryTrade =>
                        queryTrade.Transaction != null
                        && queryTrade.Transaction.Id == transaction.Id);

                    if (matchingBuy != null)
                    {
                        var feeDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
                        foreach (var fee in matchingBuy.Fees ?? new List<CoinbaseFee>())
                        {
                            if (!feeDictionary.ContainsKey(fee.Amount.Currency))
                            {
                                feeDictionary[fee.Amount.Currency] = 0;
                            }

                            feeDictionary[fee.Amount.Currency] += fee.Amount.Amount ?? 0;
                        }

                        if (feeDictionary.Keys.Any())
                        {
                            var key = feeDictionary.Keys.First();

                            historyItem.FeeCommodity = key;
                            historyItem.FeeQuantity = feeDictionary[key];
                        }

                        /*
		                    "amount": {
			                    "amount": "2.00000000",
			                    "currency": "LTC"
		                    },
		                    "total": {
			                    "amount": "418.48",
			                    "currency": "USD"
		                    },
		                    "subtotal": {
			                    "amount": "402.42",
			                    "currency": "USD"
		                    },
                        */

                        if (historyItem.NativeId == "53ed55f8-5fb5-5e8b-b5d4-ad17dcb89ffb")
                        {
                            Console.WriteLine("asdfasdf");
                        }

                        if (matchingBuy.Subtotal != null
                            && matchingBuy.Subtotal.Amount.HasValue
                            && matchingBuy.Amount != null
                            && matchingBuy.Amount.Amount.HasValue
                            && matchingBuy.Amount.Amount.Value != 0)
                        {
                            historyItem.Price = matchingBuy.Subtotal.Amount.Value / matchingBuy.Amount.Amount.Value;
                        }
                    }

                    var matchingSell = sells.SingleOrDefault(queryTrade =>
                        queryTrade.Transaction != null
                        && queryTrade.Transaction.Id == transaction.Id);

                    if (matchingSell != null)
                    {
                        var feeDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
                        foreach (var fee in matchingSell.Fees ?? new List<CoinbaseFee>())
                        {
                            if (!feeDictionary.ContainsKey(fee.Amount.Currency))
                            {
                                feeDictionary[fee.Amount.Currency] = 0;
                            }

                            feeDictionary[fee.Amount.Currency] += fee.Amount.Amount ?? 0;
                        }

                        if (feeDictionary.Keys.Any())
                        {
                            var key = feeDictionary.Keys.First();

                            historyItem.FeeCommodity = key;
                            historyItem.FeeQuantity = feeDictionary[key];
                        }

                        /*
		                    "amount": {
			                    "amount": "2.00000000",
			                    "currency": "LTC"
		                    },
		                    "total": {
			                    "amount": "418.48",
			                    "currency": "USD"
		                    },
		                    "subtotal": {
			                    "amount": "402.42",
			                    "currency": "USD"
		                    },
                        */

                        if (historyItem.NativeId == "53ed55f8-5fb5-5e8b-b5d4-ad17dcb89ffb")
                        {
                            Console.WriteLine("asdfasdf");
                        }

                        if (matchingSell.Subtotal != null
                            && matchingSell.Subtotal.Amount.HasValue
                            && matchingSell.Amount != null
                            && matchingSell.Amount.Amount.HasValue
                            && matchingSell.Amount.Amount.Value != 0)
                        {
                            historyItem.Price = matchingSell.Subtotal.Amount.Value / matchingSell.Amount.Amount.Value;
                        }
                    }

                    // Transfers within the same coinbase user show up as usd-to-usd.
                    // This isn't part of the history we're looking to track right now.
                    if (!(string.Equals(historyItem.Symbol, "USD", StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(historyItem.Symbol, "USD", StringComparison.InvariantCultureIgnoreCase)))
                    {

                        historyItems.Add(historyItem);
                    }
                }
            }

            return historyItems;
        }

        public List<HistoricalTrade> GetHistoryFromJsonOld()
        {
            var accountsWithTransactions = GetJsonHistory();

            var transactions = accountsWithTransactions.SelectMany(item => item.Transactions).ToList();
            var historyItems = new List<HistoricalTrade>();
            foreach (var transaction in transactions)
            {
                var coinbaseTransactionTypeDictionary = new Dictionary<string, CoinbaseTransactionTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "sell", CoinbaseTransactionTypeEnum.Sell },
                    { "buy", CoinbaseTransactionTypeEnum.Buy },
                    { "send", CoinbaseTransactionTypeEnum.Send },
                    { "fiat_withdrawal", CoinbaseTransactionTypeEnum.FiatWithdraw }
                };

                var coinbaseTransactionType = coinbaseTransactionTypeDictionary.ContainsKey(transaction.Type)
                    ? coinbaseTransactionTypeDictionary[transaction.Type]
                    : CoinbaseTransactionTypeEnum.Unknown;

                var walletAddress = "";
                if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.FiatWithdraw)
                {
                    walletAddress = transaction.Details?.PaymentMethodName;
                }
                else if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.Send)
                {
                    walletAddress = transaction.To?.Address;
                }

                var tradeTypeDictionary = new Dictionary<CoinbaseTransactionTypeEnum, TradeTypeEnum>()
                {
                    { CoinbaseTransactionTypeEnum.Sell, TradeTypeEnum.Sell },
                    { CoinbaseTransactionTypeEnum.Buy, TradeTypeEnum.Buy },
                    { CoinbaseTransactionTypeEnum.Send, TradeTypeEnum.Withdraw },
                    { CoinbaseTransactionTypeEnum.FiatWithdraw, TradeTypeEnum.Withdraw }
                };

                var tradeType = tradeTypeDictionary.ContainsKey(coinbaseTransactionType)
                    ? tradeTypeDictionary[coinbaseTransactionType]
                    : TradeTypeEnum.Unknown;

                var symbol = transaction?.Amount?.Currency;
                var baseSymbol = transaction?.NativeAmount?.Currency;
                var tradingPair = !string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(baseSymbol)
                    ? new TradingPair(symbol, baseSymbol)
                    : null;

                decimal? price = null;
                if (transaction?.Amount?.Amount != null && transaction?.NativeAmount?.Amount != null)
                {
                    if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.Buy)
                    {
                        price = transaction.NativeAmount.Amount / transaction.Amount.Amount;
                    }
                    else if (coinbaseTransactionType == CoinbaseTransactionTypeEnum.Buy)
                    {
                        price = transaction.Amount.Amount / transaction.NativeAmount.Amount;
                    }
                }

                var hash = transaction?.Network?.Hash;

                var feeQuantity = transaction.Network?.TransactionFee?.Amount;
                var feeCommodity = transaction.Network?.TransactionFee?.Currency;

                var historyItem = new HistoricalTrade
                {
                    NativeId = transaction.Id.ToString(),
                    Quantity = Math.Abs(transaction.Amount?.Amount ?? default(decimal)),
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    TradingPair = tradingPair,
                    TimeStampUtc = transaction.CreatedAt ?? default(DateTime),
                    TradeType = tradeType,
                    WalletAddress = walletAddress,
                    Price = price ?? default(decimal),
                    TransactionHash = hash,
                    FeeQuantity = feeQuantity ?? default(decimal),
                    FeeCommodity = feeCommodity
                };

                // historyItem.fee

                historyItems.Add(historyItem);
            }

            return historyItems;
        }

        public HistoryContainer GetUserTradeHistoryV2(CachePolicy cachePolicy)
        {
            var fileInfo = new FileInfo(HistoryTextFileName);

            var contents = fileInfo.Exists
                ? File.ReadAllText(HistoryTextFileName)
                : null;

            if (string.IsNullOrWhiteSpace(contents))
            {
                return new HistoryContainer
                {
                    AsOfUtc = null,
                    History = new List<HistoricalTrade>()
                };
            }

            var jsonHistory = GetHistoryFromJson();

            var lastWriteTime = fileInfo.LastWriteTimeUtc;

            return new HistoryContainer
            {
                AsOfUtc = lastWriteTime,
                History = jsonHistory
            };
        }

        public HistoryContainer GetUserTradeHistoryV2Old(CachePolicy cachePolicy)
        {
            var fileInfo = new FileInfo(HistoryTextFileName);

            var contents = fileInfo.Exists
                ? File.ReadAllText(HistoryTextFileName)
                : null;

            if (string.IsNullOrWhiteSpace(contents))
            {
                return new HistoryContainer
                {
                    AsOfUtc = null,
                    History = new List<HistoricalTrade>()
                };
            }

            var lastWriteTime = fileInfo.LastWriteTimeUtc;

            var history = !string.IsNullOrWhiteSpace(contents)
                ? ParseHistoricalTrades(contents)
                : new List<HistoricalTrade>();

            return new HistoryContainer
            {
                AsOfUtc = lastWriteTime,
                History = history
            };
        }

        private TradingPair ToTradingPair(string nativeSymbol, string nativeBaseSymbol)
        {
            var canon = _map.GetCanon(nativeSymbol);
            var baseCanon = _map.GetCanon(nativeBaseSymbol);

            return new TradingPair
            {
                CanonicalCommodityId = canon?.Id,
                Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                NativeSymbol = nativeSymbol,
                CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeSymbol,
                NativeCommodityName = nativeSymbol,

                CanonicalBaseCommodityId = baseCanon?.Id,
                BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                NativeBaseSymbol = nativeBaseSymbol,
                BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Name : nativeBaseSymbol,
                NativeBaseCommodityName = nativeBaseSymbol
            };
        }
    }
}
