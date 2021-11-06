using binance_lib;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_api.Controllers
{
    public class ListingController : ApiController
    {
        private readonly IBinanceTwitterReader _binanceTwitterReader;
        public ListingController(IBinanceTwitterReader binanceTwitterReader)
        {
            _binanceTwitterReader = binanceTwitterReader;
        }

        [HttpGet]
        [HttpPost]
        [Route("api/get-binance-listings")]
        public HttpResponseMessage GetBinanceListings()
        {
            var vm = _binanceTwitterReader.GetBinanceListingTweets();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
