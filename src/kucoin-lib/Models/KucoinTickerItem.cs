using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kucoin_lib.Models
{
    public class KucoinTickerItem
    {
        //"coinType": "BTC",
        public string CoinType { get; set; }

        //"trading": true,
        public bool Trading { get; set; }

        //"symbol": "BTC-USDT",
        public string Symbol { get; set; }

        //"lastDealPrice": 7970.39,
        //"buy": 7970.39,
        //"sell": 7975.0,
        //"change": -349.636456,
        //"coinTypePair": "USDT",
        public string CoinTypePair { get; set; }

        //"sort": 100,
        //"feeRate": 0.001,
        public decimal FeeRate { get; set; }

        //"volValue": 1585101.74447382,
        //"high": 8414.999407,
        //"datetime": 1523908842000,
        //"vol": 196.06889314,
        //"low": 7863.74599,
        //"changeRate": -0.042
    }
}
