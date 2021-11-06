using System.Collections.Generic;
using trade_model;

namespace binance_lib
{
    public interface IBinanceTwitterReader
    {
        List<CommodityListing> GetBinanceListingTweets();
    }
}
