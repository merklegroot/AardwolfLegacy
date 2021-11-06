using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_res;

namespace trade_api.Controllers
{
    public class CanonController : ApiController
    {
        private List<Commodity> Read()
        {
            return CommodityRes.All;
        }

        private void Write(List<Commodity> items)
        {
            CommodityRes.Write(items);
        }

        [HttpPost]
        [Route("api/get-canon")]
        public HttpResponseMessage GetCanon()
        {
            var canon = Read();
            return Request.CreateResponse(HttpStatusCode.OK, canon.OrderBy(item => item.Symbol));
        }

        public class GetCanonItemServiceModel
        {
            public Guid Id { get; set; }
        }

        [HttpPost]
        [Route("api/get-canon-item")]
        public HttpResponseMessage GetCanonItem(GetCanonItemServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (serviceModel.Id == default(Guid)) { throw new ArgumentNullException(nameof(serviceModel.Id)); }

            var allCanon = Read();
            var match = allCanon.SingleOrDefault(item => item.Id == serviceModel.Id);

            return Request.CreateResponse(HttpStatusCode.OK, match);
        }

        [HttpPost]
        [Route("api/save-canon")]
        public HttpResponseMessage SaveCanon(Commodity serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Symbol))
            {
                throw new ArgumentNullException($"{nameof(serviceModel.Symbol)} must not be null.");
            }

            var existing = Read();

            if (serviceModel.Id == default(Guid))
            {
                var commodityToCreate = new Commodity
                {
                    Id = Guid.NewGuid(),
                    ContractId = serviceModel.ContractId,
                    Decimals = serviceModel.Decimals,
                    IsDominant = serviceModel.IsDominant,
                    IsEth = serviceModel.IsEth,
                    IsEthToken = serviceModel.IsEthToken,
                    Name = serviceModel.Name,
                    Symbol = serviceModel.Symbol,
                    Website = serviceModel.Website,
                    Telegram = serviceModel.Telegram
                };

                existing.Add(commodityToCreate);
                Write(existing);
            }
            else
            {
                var match = existing.SingleOrDefault(item => item.Id == serviceModel.Id);
                if (match != null)
                {
                    match.ContractId = serviceModel.ContractId;
                    match.Decimals = serviceModel.Decimals;
                    match.IsDominant = serviceModel.IsDominant;
                    match.IsEth = serviceModel.IsEth;
                    match.IsEthToken = serviceModel.IsEthToken;
                    match.Name = serviceModel.Name;
                    match.Symbol = serviceModel.Symbol;                                       
                    match.Website = serviceModel.Website;
                    match.Telegram = serviceModel.Telegram;

                    Write(existing);
                }
            }

            return GetCanon();
        }
        
        public class MapCanonServiceModel
        {
            public string Exchange { get; set; }
            public string NativeSymbol { get; set; }
            public Guid CanonicalId { get; set; }
        }

        [HttpPost]
        [Route("api/map-canon")]
        public HttpResponseMessage MapCanon(MapCanonServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }
            if (string.IsNullOrWhiteSpace(serviceModel.NativeSymbol)) { throw new ArgumentNullException(nameof(serviceModel.NativeSymbol)); }
            if (serviceModel.CanonicalId == Guid.Empty) { throw new ArgumentNullException(nameof(serviceModel.CanonicalId)); }

            var filePortionDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "qryptos", @"qryptos-lib\Res\qryptos-map.json" },
                { "coss", @"coss-lib\Res\coss-map.json" },
                { "kucoin", @"kucoin-lib\res\kucoin-map.json" },
                { "bit-z", @"bit-z-lib\res\bitz-map.json" },
                { "binance", @"binance-lib\res\binance-map.json" },
                { "cryptopia", @"cryptopia-lib\res\cryptopia-map.json" },
                { "livecoin", @"livecoin-lib\res\livecoin-map.json" },
                { "hitbtc", @"hitbtc-lib\res\hitbtc-map.json" },
                { "yobit", @"yobit-lib\res\yobit-map.json" },
                { "oex", @"oex-lib\res\oex-map.json" }
            };

            if (!filePortionDictionary.ContainsKey(serviceModel.Exchange))
            {
                throw new NotImplementedException($"Not implemented for exchange \"${serviceModel.Exchange}\"");
            }

            const string RepoRoot = @"C:\repo\trade-ex\";
            var fileName = Path.Combine(RepoRoot, filePortionDictionary[serviceModel.Exchange]);
           
            if (!new FileInfo(fileName).Directory.Exists)
            {
                throw new ApplicationException(@"Path not found. Are you sure you're on the dev?");
            }

            var existingContents = File.Exists(fileName) ? File.ReadAllText(fileName) : null;
            var map = !string.IsNullOrWhiteSpace(fileName) ? JsonConvert.DeserializeObject<List<CommodityMapItem>>(existingContents) : new List<CommodityMapItem>();

            if (!map.Any(item => 
                item.CanonicalId == serviceModel.CanonicalId
                && string.Equals(item.NativeSymbol, serviceModel.NativeSymbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                map = map.Where(item =>
                    item.CanonicalId != serviceModel.CanonicalId
                    && !string.Equals(item.NativeSymbol, serviceModel.NativeSymbol, StringComparison.InvariantCultureIgnoreCase))
                 .ToList();

                map.Add(new CommodityMapItem { CanonicalId = serviceModel.CanonicalId, NativeSymbol = serviceModel.NativeSymbol.ToUpper() });

                var updatedContents = JsonConvert.SerializeObject(map, Formatting.Indented);

                File.WriteAllText(fileName, updatedContents);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}