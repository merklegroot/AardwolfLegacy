using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_util;

namespace binance_lib
{
    public class BinanceListingRetriever
    {
        private readonly IWebUtil _webUtil;

        public BinanceListingRetriever()
        {
            _webUtil = new WebUtil();
        }

        public void Execute()
        {
            //const string Url = "https://support.binance.com/hc/en-us/sections/115000106672-New-Listings";
            //var contents = _webUtil.Get(Url);

            //Console.WriteLine(contents);

            throw new NotImplementedException();
        }
    }
}
