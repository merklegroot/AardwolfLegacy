using Newtonsoft.Json;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace trade_res
{
    public static class CommodityRes
    {
        public static Commodity Bitcoin { get { return BySymbol("BTC"); } }

        public static Commodity Eth { get { return BySymbol("ETH"); } }

        public static Commodity EthereumClassic { get { return BySymbolAndId("ETC", new Guid("a44d6544-4ce7-4bef-86bc-778fd4cf41eb")); } }

        public static Commodity Aeron { get { return BySymbol("ARN"); } }

        public static Commodity Agrello { get { return BySymbolAndContract("DLT", "0x07e3c70653548b04f0a75970c1f81b4cbbfb606f"); } }

        public static Commodity Bezop { get { return BySymbolAndContract("BEZ", "0x3839d8ba312751aa0248fed6a8bacb84308e20ed"); } }

        public static Commodity CanYaCoin { get { return BySymbolAndContract("CAN", "0x1d462414fe14cf489c7a21cac78509f4bf8cd7c0"); } }

        public static Commodity Sonm { get { return BySymbol("SNM"); } }

        public static Commodity Dash { get { return BySymbol("DASH"); } }

        public static Commodity Dent { get { return BySymbolAndContract("DENT", "0x3597bfd533a99c9aa083587b074434e61eb0a258"); } }

        public static Commodity District0x { get { return BySymbolAndContract("DNT", "0x0abdace70d3790235af448c88547603b945604ea"); } }

        public static Commodity Fortuna { get { return BySymbolAndContract("FOTA", "0x4270bb238f6dd8b1c3ca01f96ca65b2647c06d3c"); } }

        public static Commodity Poe { get { return BySymbol("POE"); } }

        public static Commodity Substratum { get { return BySymbol("SUB"); } }

        public static Commodity PayTenX { get { return BySymbol("PAY"); } }

        public static Commodity Ark { get { return BySymbol("ARK"); } }

        public static Commodity BancorNetworkToken { get { return BySymbolAndId("BNT", new Guid("adfe8dca-c0df-423c-890d-c99bd396a0fc")); } }

        public static Commodity BitcoinCash { get { return BySymbol("BCH"); } }

        public static Commodity Iconomi { get { return BySymbolAndContract("ICN", "0x888666ca69e0f178ded6d75b5726cee99a87d698"); } }

        public static Commodity QuarkChain { get { return BySymbol("QKC"); } }

        public static Commodity NCash { get { return BySymbol("NCASH"); } }

        public static Commodity Monaco { get { return BySymbol("MCO"); } }

        public static Commodity Enigma { get { return BySymbolAndContract("ENG", "0xf0ee6b27b759c9893ce4f094b49ad28fd15a23e4"); } }

        public static Commodity Lend { get { return BySymbolAndContract("LEND", "0x80fb784b7ed66730e8b1dbd9820afd29931aab03"); } }

        public static Commodity Ontology { get { return BySymbolAndId("ONT", new Guid("70331C8E-1417-48C6-9619-85BA08B8E46F")); } }

        public static Commodity Lampix { get { return BySymbolAndId("PIX", new Guid("20AD6618-7FAA-4021-9E3E-FECF94AD8DBC")); } }

        public static Commodity RequestNetwork { get { return BySymbolAndContract("REQ", "0x8f8221afbb33998d8584a2b05749ba73c37a938a"); } }

        public static Commodity Populous { get { return BySymbolAndContract("PPT", "0xd4fa1460f537bb9085d22c7bccb5dd450ef28e3a"); } }

        public static Commodity EnjinCoin { get { return BySymbolAndContract("ENJ", "0xf629cbd94d3791c9250152bd8dfbdf380e2a3b9c"); } }

        public static Commodity Vezt { get { return BySymbolAndContract("VZT", "0x9720b467a710382a232a32f540bdced7d662a10b"); } }

        public static Commodity LaToken { get { return BySymbolAndContract("LA", "0xe50365f5d679cb98a1dd62d6f6e58e59321bcddf"); } }

        public static Commodity Ambrosous { get { return BySymbolAndContract("AMB", "0x4dc3643dbc642b72c158e7f3d2ff232df61cb6ce"); } }

        public static Commodity DataStreamer { get { return BySymbolAndContract("DATA", "0x0cf0ee63788a0849fe5297f3407f701e122cc023"); } }

        public static Commodity Omisego { get { return BySymbolAndId("OMG", new Guid(" 7bacdd56-10b2-40f7-8035-b7ccfcc555e9")); } }

        public static Commodity VeChain { get { return BySymbolAndId("VEN", new Guid("d198f027-0321-48d1-881e-08c76b3b2f11")); } }

        public static Commodity Lisk { get { return BySymbol("LSK"); } }

        public static Commodity Mana { get { return BySymbol("MANA"); } }

        public static Commodity IoTX { get { return BySymbol("IOTX"); } }

        public static Commodity Bnb { get { return BySymbolAndContract("BNB", "0xb8c77482e45f1f44de1745f52c74426c631bdd52"); } }

        public static Commodity Knc { get { return BySymbolAndContract("KNC", "0xdd974d5c2e2928dea5f71b9825b8b646686bd200"); } }

        public static Commodity LaLaWorld { get { return BySymbolAndId("LALA", new Guid("ede838a4-3e49-4aef-9a59-9b23954ef45a")); } }

        public static Commodity Cardano { get { return BySymbol("ADA"); } }

        public static Commodity Fuel { get { return BySymbolAndContract("FUEL", "0xea38eaa3c86c8f9b751533ba2e562deb9acded40"); } }

        public static Commodity AdHive { get { return BySymbolAndContract("ADH", "0xe69a353b3152dd7b706ff7dd40fe1d18b7802d31"); } }

        public static Commodity MatrixAi { get { return BySymbolAndContract("MAN", "0xe25bcec5d3801ce3a794079bf94adf1b8ccd802d"); } }

        public static Commodity Aion { get { return BySymbolAndContract("AION", "0x4ceda7906a5ed2179785cd3a40a69ee8bc99c466"); } }

        public static Commodity Wings { get { return BySymbolAndContract("WINGS", "0x667088b212ce3d06a1b553a7221e1fd19000d9af"); } }

        public static Commodity WaltonChain { get { return BySymbolAndContract("WTC", "0xb7cb1c96db6b22b0d3d9536e0108d062bd488f74"); } }

        public static Commodity Waves { get { return BySymbolAndId("WAVES", new Guid("eaac485d-fce9-42c4-9170-8fef7b50d441")); } }

        public static Commodity Everex { get { return BySymbolAndContract("EVX", "0xf3db5fa2c66b7af3eb0c0b782510816cbe4813b8"); } }

        public static Commodity Bluzelle { get { return BySymbolAndContract("BLZ", "0x5732046a883704404f284ce41ffadd5b007fd668"); } }

        public static Commodity BlazeCoin { get { return BySymbolAndId("BLZ", new Guid("2b706e82-1774-44e9-99f7-66b618cfe00f")); } }

        public static Commodity SaltLending { get { return BySymbolAndContract("SALT", "0x4156d3342d5c385a87d264f90653733592000581"); } }

        public static Commodity Rlc { get { return BySymbolAndContract("RLC", "0x607f4c5bb672230e8672085532f7e901544a7375"); } }

        public static Commodity Augur { get { return BySymbolAndContract("REP", "0xe94327d07fc17907b4db788e5adf2ed424addff6"); } }

        public static Commodity Stox { get { return BySymbolAndContract("STX", "0x006bea43baa3f7a6f765f14f10a1a1b08334ef45"); } }

        public static Commodity Utrust { get { return BySymbolAndContract("UTK", "0x70a72833d6bf7f508c8224ce59ea1ef3d0ea3a38"); } }

        public static Commodity Havven { get { return BySymbolAndContract("HAV", "0xf244176246168f24e3187f7288edbca29267739b"); } }

        public static Commodity Asch { get { return BySymbolAndId("XAS", new Guid("1b6cf35a-e7cd-4f6e-a0ce-ef4501648cf3")); } }

        public static Commodity Penta { get { return BySymbolAndId("PNT", new Guid("5c09716e-e4eb-41be-9092-b8f1ea117695")); } }

        public static Commodity TheKey { get { return BySymbolAndId("TKY", new Guid("d4d9d67e-3708-4cf8-b111-7caa1f8864e7")); } }

        public static Commodity Indahash { get { return BySymbolAndId("IDH", new Guid("b31461b0-5df3-4f2f-b908-82be4dc74a34")); } }

        public static Commodity Civic { get { return BySymbolAndId("CVC", new Guid("dca3998f-b96c-4026-96c2-2c6b7ac1315b")); } }

        public static Commodity NewEconomyMovement { get { return BySymbolAndId("XEM", new Guid("6a05c4bf-f3d6-4720-82eb-440449ef322c")); } }

        public static Commodity FundYourselfNow { get { return BySymbolAndId("FYN", new Guid("29898a7e-4259-41cb-831f-a2064084f22a")); } }

        public static Commodity Nitro { get { return BySymbolAndId("NOX", new Guid("6d75476a-166f-4f22-9877-7b2a9c7d8307")); } }

        public static Commodity ZenCash { get { return BySymbolAndId("ZEN", new Guid("ab771ee2-85ff-41a6-87f3-8a2e178fe982")); } }

        public static Commodity UnikoinGold { get { return BySymbolAndId("UKG", new Guid("7ac064b8-58a2-4d3c-bdd9-074c37cb1466")); } }

        public static Commodity OysterPearl { get { return BySymbolAndId("PRL", new Guid("57d76a18-ad27-49b8-8f53-c8bafebe93e5")); } }

        public static Commodity IxLedger { get { return BySymbolAndId("IXT", new Guid("9699707e-8e56-47c3-a24f-42897d787817")); } }

        public static Commodity Cs { get { return BySymbolAndId("CS", new Guid("022e8070-62f3-4fbf-b282-9490ef65f866")); } }

        public static Commodity Qash { get { return BySymbolAndId("QASH", new Guid("8ab5a595-ce3c-444b-823d-87a785d60070")); } }

        public static Commodity ByEthContract(string contract)
        {
            if (string.IsNullOrWhiteSpace(contract)) { throw new ArgumentNullException(nameof(contract)); }
            return All.SingleOrDefault(item => string.Equals(item.ContractId, contract, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Commodity ById(Guid id)
        {
            if (id == default(Guid)) { throw new ArgumentException($"{nameof(id)} must not be empty."); }

            EnsureAll();
            return _byId != null && _byId.ContainsKey(id) ? _byId[id] : null;
        }

        public static Commodity BySymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            EnsureAll();
            return _bySymbol != null && _bySymbol.ContainsKey(symbol) ? _bySymbol[symbol].SingleOrDefault() : null;
        }

        public static List<Commodity> BySymbolAllowMultiple(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            EnsureAll();
            return _bySymbol != null && _bySymbol.ContainsKey(symbol) ? _bySymbol[symbol] : new List<Commodity>();
        }

        public static Commodity BySymbolAndContract(string symbol, string contract)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(contract)) { throw new ArgumentNullException(nameof(contract)); }

            EnsureAll();

            var matches = _bySymbol != null && _bySymbol.ContainsKey(symbol) 
                ? _bySymbol[symbol]
                    .Where(item => string.Equals(item.ContractId, contract, StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
                : new List<Commodity>();

            if (!matches.Any()) { return null; }
            if (matches.Count == 1) { return matches.Single(); }

            throw new ApplicationException($"Got multiple matches for symbol {symbol} and contract {contract}.");
        }

        public static Commodity BySymbolAndId(string symbol, Guid id)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (id == default(Guid)) { throw new ArgumentNullException(nameof(id)); }

            EnsureAll();
            var match = ById(id);

            return match != null && string.Equals(match.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                ? match
                : null;
        }

        // a silly way to make sure that it's been accessed at least once.
        private static void EnsureAll()
        {
            var x = All;
        }

        private const string FileName = @"C:\repo\trade-ex\trade-res\Resources\canon.json";
        private static object Locker = new object();
        private static DateTime? _memoryLastWriteTimeUtc = null;
        public static List<Commodity> All
        {
            get
            {
                lock (Locker)
                {
                    var fillDictionaries = new Action<List<Commodity>>(all =>
                    {
                        var byId = new Dictionary<Guid, Commodity>();
                        var bySymbol = new Dictionary<string, List<Commodity>>(StringComparer.InvariantCultureIgnoreCase);
                        foreach (var item in all)
                        {
                            byId[item.Id] = item;
                            if (!bySymbol.ContainsKey(item.Symbol)) { bySymbol[item.Symbol] = new List<Commodity>(); }
                            bySymbol[item.Symbol].Add(item);
                        }

                        _byId = byId;
                        _bySymbol = bySymbol;
                    });

                    // for the dev machine...
                    if (File.Exists(FileName))
                    {
                        var fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(FileName);
                        if (_all != null && _memoryLastWriteTimeUtc.HasValue
                            && _memoryLastWriteTimeUtc.Value == fileLastWriteTimeUtc)
                        {
                            return _all;
                        }                       

                        var contents = File.ReadAllText(FileName);
                        var all = JsonConvert.DeserializeObject<List<Commodity>>(contents);
                        fillDictionaries(all);

                        _all = all;
                        _memoryLastWriteTimeUtc = fileLastWriteTimeUtc;
                        return _all;
                    }

                    if (_all != null) { return _all; }
                    lock (ResourceLocker)
                    {
                        if (_all != null) { return _all; }
                        var all = ResUtil.Get<List<Commodity>>("canon.json", typeof(TradeResDummy).Assembly);
                        fillDictionaries(all);
                        _all = all;
                    }

                    return _all;
                }
            }
        }

        public static void Write(List<Commodity> items)
        {
            lock (Locker)
            {
                var contents = JsonConvert.SerializeObject(items ?? new List<Commodity>(), Formatting.Indented);
                File.WriteAllText(FileName, contents);
            }
        }

        private static List<Commodity> _all = null;
        private static Dictionary<Guid, Commodity> _byId = null;
        private static Dictionary<string, List<Commodity>> _bySymbol = null;
        private static object ResourceLocker = new object();
        
    }
}
