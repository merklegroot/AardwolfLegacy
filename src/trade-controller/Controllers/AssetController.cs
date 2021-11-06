using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_model;
using trade_res;

namespace trade_web.Controllers
{
    public class AssetController : BaseController
    {
        [HttpGet]
        [Route("api/get-estimated-values")]
        public HttpResponseMessage GetEstimatedValues()
        {
            var symbols = new List<string> { "BTC", "ETC", "LTC" };
            var vm = symbols.Select(item => new { Symbol = item, Prices = _cryptoCompareRepo.GetPrices(item) });

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpGet]
        [Route("api/assets")]
        public HttpResponseMessage GetAssets()
        {
            var assets = Asset.All.OrderBy(item => item.Symbol).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, assets);
        }

        [HttpGet]
        [Route("api/get-asset-detail")]
        public HttpResponseMessage GetAssetDetail(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException("symbol"); }

            var effectiveSymbol = (symbol ?? string.Empty).Trim().ToUpper();
            var asset = Asset.All.FirstOrDefault(item => string.Equals(item.Symbol, (effectiveSymbol ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase));

            if (asset == null) { throw new ApplicationException("Asset not found."); }

            var prices = _cryptoCompareRepo.GetPrices(asset.Symbol);
            var ethToBtc = _cryptoCompareRepo.GetPrices("ETH").Single(item => string.Equals(item.Key, "BTC", StringComparison.InvariantCultureIgnoreCase)).Value;

            var exchangeOrderBooks = new List<ExchangeOrderBook>();

            var binanceOrderBooks = new List<OrderBook>();
            binanceOrderBooks.Add(_binanceIntegration.GetOrderBook(new TradingPair(asset.Symbol, "ETH")));
            binanceOrderBooks.Add(_binanceIntegration.GetOrderBook(new TradingPair(asset.Symbol, "BTC")));
            exchangeOrderBooks.Add(new ExchangeOrderBook { Name = "Binance", OrderBooks = binanceOrderBooks });

            var cossOrderBooks = new List<OrderBook>();
            cossOrderBooks.Add(_cossIntegration.GetOrderBook(new TradingPair(asset.Symbol, "ETH")));
            cossOrderBooks.Add(_cossIntegration.GetOrderBook(new TradingPair(asset.Symbol, "BTC")));
            exchangeOrderBooks.Add(new ExchangeOrderBook { Name = "Coss", OrderBooks = cossOrderBooks });

            var bitzOrderBooks = new List<OrderBook>();

            var bitzTradingPairs = _bitzIntegration.GetTradingPairs();
            if (bitzTradingPairs.Any(item => 
                string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.Symbol, asset.Symbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                bitzOrderBooks.Add(_bitzIntegration.GetOrderBook(new TradingPair(asset.Symbol, "BTC")));
            }

            if (bitzOrderBooks.Any())
            {
                exchangeOrderBooks.Add(new ExchangeOrderBook { Name = "Bit-Z", OrderBooks = bitzOrderBooks });
            }

            var viewModel = new AssetWithPrices(asset.Symbol, asset.Name, asset.ContractId);
            viewModel.Prices = prices;
            viewModel.ExchangeOrderBooks = exchangeOrderBooks;
            viewModel.EthToBtc = ethToBtc;

            return Request.CreateResponse(HttpStatusCode.OK, viewModel);
        }

        private class CacheItem
        {
            public DateTime TimeStamp { get; set; }
            public string Contents { get; set; }
        }

        private static Dictionary<string, CacheItem> Cache = new Dictionary<string, CacheItem>();        

        public class AssetWithPrices : Asset
        {
            public AssetWithPrices(string symbol, string name = null, string contractId = null) : base(symbol, name, contractId)
            {
            }

            public Dictionary<string, decimal> Prices { get; set; }
            
            public List<ExchangeOrderBook> ExchangeOrderBooks { get; set; }

            public decimal EthToBtc { get; set; }
        }

        public class ExchangeOrderBook
        {
            public string Name { get; set; }
            public List<OrderBook> OrderBooks { get; set; }
        }
    }
}
