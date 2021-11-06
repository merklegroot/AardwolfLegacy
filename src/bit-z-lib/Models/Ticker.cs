//using Newtonsoft.Json.Linq;
//using System.Collections.Generic;
//using trade_model;

//namespace bit_z_lib
//{
//    public class Ticker : BitzResponse
//    {
//        public JObject Data { get; set; }

//        public List<TickerData> Items
//        {
//            get
//            {
//                var items = new List<TickerData>();

//                foreach (var token in Data.AsJEnumerable())
//                {                    
//                    var item = new TickerData();
//                    item.Pair = token.Path;
//                    item.Date = token.First["date"].Value<long>();
//                    item.Last = token.First["last"].Value<decimal>();
//                    item.Buy = token.First["buy"].Value<decimal>();
//                    item.High = token.First["high"].Value<decimal>();
//                    item.Low = token.First["low"].Value<decimal>();
//                    item.Volume = token.First["vol"].Value<decimal>();

//                    items.Add(item);
//                }

//                return items;
//            }
//        }

//        public class TickerData
//        {
//            public string Symbol
//            {
//                get
//                {
//                    if (string.IsNullOrWhiteSpace(Pair)) { return null; }
//                    var pieces = Pair.Split('_');
//                    return pieces[0].Trim().ToUpper();
//                }
//            }

//            public string BaseSymbol
//            {
//                get
//                {
//                    if (string.IsNullOrWhiteSpace(Pair)) { return null; }
//                    var pieces = Pair.Split('_');
//                    return pieces[1].Trim().ToUpper();
//                }
//            }

//            public TradingPair TradingPair { get { return new TradingPair { Symbol = Symbol, BaseSymbol = BaseSymbol }; } }
//            public string Pair { get; set; }
//            public long Date { get; set; }
//            public decimal Last { get; set; }
//            public decimal Buy { get; set; }
//            public decimal High { get; set; }
//            public decimal Low { get; set; }
//            public decimal Volume { get; set; }
//        }
//    }
//}
