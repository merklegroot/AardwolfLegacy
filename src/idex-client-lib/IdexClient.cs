using System;
using System.Collections.Generic;
using web_util;

namespace idex_client_lib
{
    public class IdexClient : IIdexClient
    {
        private const string ApiBase = "https://api.idex.market";

        private readonly IWebUtil _webUtil;

        public IdexClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string PlaceLimitOrder(
            string tokenBuy,
            double amountBuy,
            string tokenSell,
            double amountSell,
            string address,
            long nonce,
            long expires,
            int v,
            string r,
            string s
            )
        {
            const string Path = "order";

            //{
            //    "tokenBuy": "0xd6e8a328c5c9b6cc4c917a50ecbe0aeb663c666e",
            //    "amountBuy": "1000000000000000000",
            //    "tokenSell": "0x0000000000000000000000000000000000000000",
            //    "amountSell": "20354156573527349",
            //    "address": "0x2dbdcec64db33e673140fbd0ceef610a273b84db",
            //    "nonce": "1544",
            //    "expires": 100000,
            //    "v": 28,
            //    "r": "0xc6ddcbdf69d0e20fe879d2405b40ee417773c8a177a5d7f4461f2310565ac3d1",
            //    "s": "0x497cdfedfde3308bb9d9e80ea2eabff43c7a15fef0eb164c265e3855a1bd9073"
            //}

            var payload = new List<KeyValuePair<string, object>>
            {
                // "tokenBuy": "0xd6e8a328c5c9b6cc4c917a50ecbe0aeb663c666e",
                new KeyValuePair<string, object>("tokenBuy", tokenBuy),

                // "amountBuy": "1000000000000000000",
                new KeyValuePair<string, object>("amountBuy", amountBuy),

                // "tokenSell": "0x0000000000000000000000000000000000000000",
                new KeyValuePair<string, object>("tokenSell", tokenSell),

                // "amountSell": "20354156573527349",
                new KeyValuePair<string, object>("amountSell", amountSell),

                // "address": "0x2dbdcec64db33e673140fbd0ceef610a273b84db",
                new KeyValuePair<string, object>("address", address),

                // "nonce": "1544",
                new KeyValuePair<string, object>("nonce", nonce),

                // "expires": 100000,
                new KeyValuePair<string, object>("expires", expires),

                // "v": 28,
                new KeyValuePair<string, object>("v", v),

                // "r": "0xc6ddcbdf69d0e20fe879d2405b40ee417773c8a177a5d7f4461f2310565ac3d1",
                new KeyValuePair<string, object>("r", r),

                // "s": "0x497cdfedfde3308bb9d9e80ea2eabff43c7a15fef0eb164c265e3855a1bd9073"
                new KeyValuePair<string, object>("s", s)
            };

            throw new NotImplementedException();
        }

        public string GetOrderBookRaw(string nativeSymbol, string nativeBaseSymbol)
        {
            var json = $"{{ \"selectedMarket\": \"{nativeBaseSymbol}\", \"tradeForMarket\": \"{nativeSymbol}\" }}";
            const string Url = "https://api.idex.market/returnOrderBookForMarket";

            return _webUtil.Post(Url, json);
        }
    }
}
