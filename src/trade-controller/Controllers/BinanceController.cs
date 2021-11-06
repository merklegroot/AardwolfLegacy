using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_res;

namespace trade_web.Controllers
{
    public class BinanceController : BaseController
    {
        [HttpGet]
        [Route("api/get-binance-asset")]
        public HttpResponseMessage GetBinanceAssetInfo(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var effectiveSymbol = (symbol ?? string.Empty).Trim().ToUpper();
            if (!Asset.All.Any(item => string.Equals(item.Symbol, effectiveSymbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ApplicationException($"Unknown symbol \"{effectiveSymbol}\".");
            }

            var withdrawlFees = _binanceIntegration.GetWithdrawlFees();
            var fee = withdrawlFees.ContainsKey(effectiveSymbol) ? withdrawlFees[effectiveSymbol] : (decimal?)null;

            return Request.CreateResponse(HttpStatusCode.OK, new {
                WithdrawlFee = fee });
        }

        [HttpPost]
        [Route("api/get-binance-deposit-addresses")]
        public HttpResponseMessage GetBinanceDepositAddresses()
        {
            var vm = _binanceIntegration.GetDepositAddresses();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
