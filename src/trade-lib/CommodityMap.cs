using Newtonsoft.Json;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using trade_lib;
using trade_model;
using trade_res;

namespace commodity_map
{
    public abstract class CommodityMap
    {
        public DateTime? TimeStampUtc => CanonCache?.TimeStampUtc;

        public string ToCanonicalSymbol(string nativeSymbol)
        {
            if (string.IsNullOrWhiteSpace(nativeSymbol)) { throw new ArgumentNullException(nameof(nativeSymbol)); }

            var effectiveNativeSymbol = nativeSymbol.Trim().ToUpper();

            var allCanon = CanonCache.AllCanon;
            var commodityMap = MapCache.MapItems;
            var nativeSymbolDictionary = MapCache.NativeSymbolDictionary;

            var mapItem = nativeSymbolDictionary.ContainsKey(effectiveNativeSymbol)
                ? nativeSymbolDictionary[effectiveNativeSymbol]
                : null;

            if (mapItem == null) { return nativeSymbol; }
            var canon = allCanon.ById(mapItem.CanonicalId);
            return !string.IsNullOrWhiteSpace(canon?.Symbol)
                ? canon.Symbol
                : nativeSymbol;
        }

        public string ToNativeSymbol(
            string canonicalSymbol)
        {
            var allCanon = CanonCache.AllCanon;
            var idDictionary = CanonCache.IdDictionary;
            var commodityMap = MapCache.MapItems;

            var canonWithMapItem = commodityMap.Select(mapItem =>
            {
                var canon = idDictionary.ContainsKey(mapItem.CanonicalId)
                    ? idDictionary[mapItem.CanonicalId]
                    : null;

                return new { MapItem = mapItem, Canon = canon };
            });

            var matches = canonWithMapItem.Where(item =>
            {
                return string.Equals(item.Canon.Symbol, canonicalSymbol, StringComparison.InvariantCultureIgnoreCase);
            }).ToList();

            var match = matches.SingleOrDefault();

            return match != null
                ? match.MapItem.NativeSymbol
                : canonicalSymbol;
        }

        public NativeTradingPair ToNativeTradingPair(TradingPair tradingPair)
        {
            if (tradingPair == null) { return null; }

            var nativeSymbol = ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = ToNativeSymbol(tradingPair.BaseSymbol);

            return new NativeTradingPair(nativeSymbol, nativeBaseSymbol);
        }

        public Commodity GetCanon(string nativeSymbol)
        {
            if (string.IsNullOrWhiteSpace(nativeSymbol)) { throw new ArgumentNullException(nativeSymbol); }

            var effectiveNativeSymbol = nativeSymbol.ToUpper().Trim();

            var allCanon = CanonCache.AllCanon;
            var idDictionary = CanonCache.IdDictionary;

            var commodityMap = MapCache.MapItems;

            var mapItem = commodityMap.SingleOrDefault(map => string.Equals(map.NativeSymbol, effectiveNativeSymbol));

            var canon = mapItem != null && idDictionary.ContainsKey(mapItem.CanonicalId)
                ? idDictionary[mapItem.CanonicalId]
                : null;

            return canon;
        }

        protected abstract string MapFilePiece { get; }

        protected abstract string MapFilePath { get; }

        protected abstract Assembly IntegrationAssembly { get; }
        
        private string MapFileName { get { return Path.Combine(MapFilePath, MapFilePiece); } }
        
        private List<CommodityMapItem> GetRes() => ResUtil.Get<List<CommodityMapItem>>(MapFilePiece, IntegrationAssembly);

        private static Dictionary<string, List<CommodityMapItem>> _cachedMapContainer = new Dictionary<string, List<CommodityMapItem>>();
        private List<CommodityMapItem> CachedMap
        {
            get
            {
                if (!_cachedMapContainer.ContainsKey(IntegrationAssembly.FullName))
                {
                    _cachedMapContainer[IntegrationAssembly.FullName] = null;
                    return null;
                }

                return _cachedMapContainer[IntegrationAssembly.FullName];
            }
            set
            {
                _cachedMapContainer[IntegrationAssembly.FullName] = value;
            }
        }

        private static Dictionary<string, DateTime?> CachedMapTimeStampContainer = new Dictionary<string, DateTime?>();
        private DateTime? CachedMapTimeStamp
        {
            get
            {
                if (!CachedMapTimeStampContainer.ContainsKey(IntegrationAssembly.FullName))
                {
                    CachedMapTimeStampContainer[IntegrationAssembly.FullName] = null;
                    return null;
                }

                return CachedMapTimeStampContainer[IntegrationAssembly.FullName];
            }
            set
            {
                CachedMapTimeStampContainer[IntegrationAssembly.FullName] = value;
            }
        }

        private static TimeSpan CachedMapThreshold = TimeSpan.FromSeconds(10);        

        private List<CommodityMapItem> GetCommodityMap()
        {
            try
            {
                if (CachedMapTimeStamp.HasValue && (DateTime.UtcNow - CachedMapTimeStamp.Value) < CachedMapThreshold)
                {
                    return CachedMap;
                }

                if (File.Exists(MapFileName))
                {
                    var contents = File.ReadAllText(MapFileName);
                    CachedMap = !string.IsNullOrWhiteSpace(contents)
                        ? JsonConvert.DeserializeObject<List<CommodityMapItem>>(contents)
                        : new List<CommodityMapItem>();

                    CachedMapTimeStamp = DateTime.UtcNow;

                    return CachedMap;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            CachedMap = GetRes();
            CachedMapTimeStamp = DateTime.UtcNow;

            return CachedMap;
        }

        private class MapContainer
        {
            public List<CommodityMapItem> MapItems { get; set; }
            public Dictionary<string, CommodityMapItem> NativeSymbolDictionary { get; set; }
            public DateTime TimeStampUtc { get; set; }
        }

        private MapContainer _mapCache = null;
        private static object MapLock = new object();
        private MapContainer MapCache
        {
            get
            {
                if (_mapCache != null && DateTime.UtcNow - _mapCache.TimeStampUtc < TimeSpan.FromSeconds(10))
                { return _mapCache; }

                lock (MapLock)
                {
                    if (_mapCache != null && DateTime.UtcNow - _mapCache.TimeStampUtc < TimeSpan.FromSeconds(10))
                    { return _mapCache; }

                    var mapItems = GetCommodityMap();
                    var nativeSymbolDictionary = new Dictionary<string, CommodityMapItem>();
                    foreach (var mapItem in mapItems ?? new List<CommodityMapItem>())
                    {
                        if (string.IsNullOrWhiteSpace(mapItem.NativeSymbol)) { continue; }
                        nativeSymbolDictionary[mapItem.NativeSymbol.Trim().ToUpper()] = mapItem;
                    }

                    return _mapCache = new MapContainer
                    {
                        MapItems = mapItems,
                        NativeSymbolDictionary = nativeSymbolDictionary,
                        TimeStampUtc = DateTime.UtcNow
                    };
                }
            }
        }

        private static CanonContainer _canonCache = null;
        private static object CanonLocker = new object();

        private class CanonContainer
        {
            public List<Commodity> AllCanon { get; set; }
            public Dictionary<Guid, Commodity> IdDictionary { get; set; }
            public DateTime TimeStampUtc { get; set; }
        }

        private static CanonContainer CanonCache
        {
            get
            {
                if (_canonCache != null && DateTime.UtcNow - _canonCache.TimeStampUtc < TimeSpan.FromSeconds(10))
                { return _canonCache; }

                lock (CanonLocker)
                {
                    if (_canonCache != null && DateTime.UtcNow - _canonCache.TimeStampUtc < TimeSpan.FromSeconds(10))
                    { return _canonCache; }

                    var allCanon = CommodityRes.All;
                    var idDictionary = new Dictionary<Guid, Commodity>();
                    foreach(var commodity in allCanon ?? new List<Commodity>())
                    {
                        idDictionary[commodity.Id] = commodity;
                    }

                    return _canonCache = new CanonContainer
                    {
                        AllCanon = allCanon,
                        IdDictionary = idDictionary,
                        TimeStampUtc = DateTime.UtcNow
                    };
                }
            }
        }
    }
}
