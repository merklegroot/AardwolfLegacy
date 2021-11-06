using config_connection_string_lib;
using dump_lib;
using idex_model;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using parse_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;
using trade_res;

namespace idex_data_lib
{
    public class IdexFrameRepo : IIdexFrameRepo
    {
        private const string EmptyToken = "0x0000000000000000000000000000000000000000";

        private const string DatabaseName = "idex";

        private readonly IGetConnectionString _getConnectionString;

        public IdexFrameRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public void Insert(IdexFrameContainer container)
        {
            RawFrameCollectionContext.Insert(container);
        }

        public void Process()
        {
            var items = RawFrameCollection.AsQueryable()
                .OrderBy(item => item.Id);

            ParsedFrameCollectionContext.DropCollection();

            var parsedFrames = new List<IdexFrame>();
            foreach (var item in items)
            {
                var rawFrameContents = item.FrameContents;
                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawFrameContents);
                var parsedFrame = new IdexFrame();
                parsedFrame.RawFrameId = item.Id;
                parsedFrame.Raw = item.FrameContents;

                var method = (string)json["method"];
                parsedFrame.Method = method;

                var payloadText = json["payload"].ToString();
                var orderMethods = new List<string> { "notifyOrderInserted", "pushCancel" };
                if (orderMethods.Any(queryOrderMethod => string.Equals(queryOrderMethod, method, StringComparison.InvariantCultureIgnoreCase)))
                {
                    parsedFrame.Payload = JsonConvert.DeserializeObject<IdexFrame.IdexFramePayload>(payloadText);
                }
                else if (string.Equals(method, "pushEthPrice"))
                {
                    parsedFrame.Payload = new IdexFrame.IdexFramePayload
                    {
                        EthPrice = ParseUtil.DecimalTryParse(payloadText)
                    };
                }

                ParsedFrameCollectionContext.Insert(parsedFrame);
                parsedFrames.Add(parsedFrame);
            }

            var state = new IdexState();
            foreach (var parsedFrame in parsedFrames)
            {
                ApplyFrame(state, parsedFrame);
            }

            var orderBooksBySymbol = new Dictionary<string, OrderBook>();
            foreach (var key in state.FramesByToken.Keys.ToList())
            {
                var commodity = CommodityRes.ByEthContract(key);
                if (commodity == null || string.IsNullOrWhiteSpace(commodity.Symbol)) { continue; }

                var frames = state.FramesByToken[key];

                var orderBook = new OrderBook { Asks = new List<Order>(), Bids = new List<Order>() };               
                foreach (var frame in frames)
                {
                    if (frame.OrderType == IdexOrder.AskOrBid.Ask)
                    {
                        if (frame.SalePrice.HasValue && frame.QuantityToSell.HasValue)
                        {
                            var order = new Order
                            {
                                Price = frame.SalePrice.Value,
                                Quantity = frame.QuantityToSell.Value
                            };

                            orderBook.Asks.Add(order);
                        }
                    }
                    else if (frame.OrderType == IdexOrder.AskOrBid.Bid)
                    {
                        if (frame.QuantityToBuy.HasValue && frame.PurchasePrice.HasValue)
                        {
                            var order = new Order
                            {
                                Price = frame.PurchasePrice.Value,
                                Quantity = frame.QuantityToBuy.Value
                            };

                            orderBook.Bids.Add(order);
                        }
                    }
                }

                orderBooksBySymbol[commodity.Symbol] = orderBook;
            }

            orderBooksBySymbol.Dump();
        }

        public class IdexOrder
        {
            private const string EmptyToken = "0x0000000000000000000000000000000000000000";

            public string Symbol => Commodity?.Symbol;
            public string BaseSymbol => BaseCommodity?.Symbol;

            public enum AskOrBid { Unknown, Ask, Bid };

            public AskOrBid OrderType
            {
                get
                {

                    if (string.Equals(EmptyToken, TokenBuy, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(EmptyToken, TokenSell, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return AskOrBid.Unknown;
                    }

                    if (!string.Equals(EmptyToken, TokenBuy, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return AskOrBid.Bid;
                    }

                    if (!string.Equals(EmptyToken, TokenSell, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return AskOrBid.Ask;
                    }

                    return AskOrBid.Unknown;
                }
            }

            public decimal? QuantityToSell
            {
                get
                {
                    if (!AmountSell.HasValue) { return null; }

                    var commodity = Commodity;
                    if (commodity == null || !commodity.Decimals.HasValue) { return null; }
                    var symbolFactor = 1.0d / Math.Pow(10.0, commodity.Decimals.Value);
                    return (decimal)(symbolFactor * AmountSell.Value);
                }
            }

            public decimal? QuantityToBuy
            {
                get
                {
                    if (!AmountBuy.HasValue) { return null; }

                    var baseCommodity = BaseCommodity;
                    if (baseCommodity == null || !baseCommodity.Decimals.HasValue) { return null; }
                    var symbolFactor = 1.0d / Math.Pow(10.0, baseCommodity.Decimals.Value);
                    return (decimal)(symbolFactor * AmountBuy.Value);
                }
            }

            public decimal? SalePrice
            {
                get
                {
                    if (!AmountSell.HasValue
                        || !AmountBuy.HasValue
                        || Commodity == null
                        || BaseCommodity == null
                        || SymbolFactor == null
                        || BaseSymbolFactor == null) { return null; }

                    if (AmountSell.Value <= 0 || AmountBuy.Value <= 0) { return 0; }

                    return (decimal)(AmountSell > 0
                        //? (BaseSymbolFactor * AmountBuy) / (SymbolFactor * AmountSell)
                        ? (BaseSymbolFactor * AmountSell) / (SymbolFactor * AmountBuy)
                        : 0);
                }
            }

            public decimal? PurchasePrice
            {
                get
                {
                    if (!AmountSell.HasValue
                        || !AmountBuy.HasValue
                        || Commodity == null
                        || BaseCommodity == null
                        || SymbolFactor == null
                        || BaseSymbolFactor == null) { return null; }

                    if (AmountSell.Value <= 0) { return 0; }

                    return (decimal)(AmountBuy > 0
                        ? (BaseSymbolFactor * AmountSell) / (SymbolFactor * AmountBuy)
                        : 0);
                }
            }

            [JsonIgnore]
            public string TokenBuy { get; set; }

            [JsonIgnore]
            public string TokenSell { get; set; }

            [JsonIgnore]
            public double? AmountSell { get; set; }

            [JsonIgnore]
            public double? AmountBuy { get; set; }

            private Commodity Commodity => !string.Equals(TokenBuy, EmptyToken) ? CommodityRes.ByEthContract(TokenBuy) : CommodityRes.Eth;

            private Commodity BaseCommodity => !string.Equals(TokenSell, EmptyToken) ? CommodityRes.ByEthContract(TokenSell) : CommodityRes.Eth;

            private double? SymbolFactor
            {
                get
                {
                    var commodity = Commodity;
                    if (commodity == null || !commodity.Decimals.HasValue) { return null; }
                    return 1.0d / Math.Pow(10.0, commodity.Decimals.Value);
                }
            }

            private double? BaseSymbolFactor
            {
                get
                {
                    var baseCommodity = BaseCommodity;
                    if (baseCommodity == null || !baseCommodity.Decimals.HasValue) { return null; }
                    return 1.0d / Math.Pow(10.0, baseCommodity.Decimals.Value);
                }
            }
        }

        public class IdexState
        {
            public decimal? EthPrice { get; set; }

            [JsonIgnore]
            public List<IdexFrame> OrderFrames = new List<IdexFrame>();

            public Dictionary<string, List<IdexOrder>> FramesByToken
            {
                get
                {
                    var framesByToken = new Dictionary<string, List<IdexOrder>>();

                    foreach (var frame in OrderFrames)
                    {
                        string effectiveToken = null;
                        if (!string.IsNullOrWhiteSpace(frame.Payload.TokenBuy)
                            && !string.Equals(frame.Payload.TokenBuy, EmptyToken))
                        {
                            effectiveToken = frame.Payload.TokenBuy;
                        }
                        else if (!string.IsNullOrWhiteSpace(frame.Payload.TokenSell)
                            && !string.Equals(frame.Payload.TokenSell, EmptyToken))
                        {
                            effectiveToken = frame.Payload.TokenSell;
                        }
                        else
                        {
                            continue;
                        }

                        var collection = framesByToken.ContainsKey(effectiveToken)
                            ? framesByToken[effectiveToken]
                            : (framesByToken[effectiveToken] = new List<IdexOrder>());

                        var idexOrder = new IdexOrder
                        {
                            TokenBuy = frame.Payload.TokenBuy,
                            TokenSell = frame.Payload.TokenSell,
                            AmountBuy = frame.Payload.AmountBuy,
                            AmountSell = frame.Payload.AmountSell
                        };

                        collection.Add(idexOrder);
                    }

                    return framesByToken;
                }
            }
        }

        private void ApplyFrame(IdexState state, IdexFrame frame)
        {
            if (frame.ParsedMethod == IdexFrame.MethodEnum.PushEthPrice)
            {
                var ethPrice = frame?.Payload?.EthPrice;
                if (ethPrice.HasValue && ethPrice.Value > 0) { state.EthPrice = ethPrice.Value; }
            }
            else if (frame.ParsedMethod == IdexFrame.MethodEnum.NotifyOrderInserted)
            {
                if (string.IsNullOrWhiteSpace(frame?.Payload?.Hash)) { return; }
                state.OrderFrames.Add(frame);
            }
            else if (frame.ParsedMethod == IdexFrame.MethodEnum.PushCancel)
            {
                state.OrderFrames = state.OrderFrames.Where(item => 
                    !string.Equals(item?.Payload?.Hash, frame?.Payload?.Hash, StringComparison.InvariantCultureIgnoreCase)
                ).ToList();
            }
        }

        public void TruncateOldData()
        {
            var threshold = TimeSpan.FromHours(2);

            var connectionString = _getConnectionString.GetConnectionString();
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(DatabaseName);

            var filter = Builders<BsonDocument>.Filter.Lt("RelayServerTimeStampUtc", DateTime.UtcNow.Add(-threshold));
            RawFrameBsonCollection.DeleteMany(filter);
        }

        private IMongoCollectionContext RawFrameCollectionContext => new MongoCollectionContext(DbContext, "idex--socket-frame");
        private IMongoCollection<IdexFrameContainer> RawFrameCollection => RawFrameCollectionContext.GetCollection<IdexFrameContainer>();
        private IMongoCollection<BsonDocument> RawFrameBsonCollection => RawFrameCollectionContext.GetCollection<BsonDocument>();

        private IMongoCollectionContext ParsedFrameCollectionContext => new MongoCollectionContext(DbContext, "idex--parsed-socket-frame");
        private IMongoCollection<IdexFrame> ParsedFramesCollection => ParsedFrameCollectionContext.GetCollection<IdexFrame>();

        private IMongoDatabaseContext DbContext => new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName);
    }
}
