using cache_lib.Models;
using exchange_client_lib;
using integration_workflow_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_res;

namespace trade_api.Controllers
{
    public class TransferFundsController : ApiController
    {
        private readonly ILogRepo _log;        
        private readonly ITransferFundsWorkflow _transferFundsWorkflow;
        private readonly IExchangeClient _exchangeClient;

        public TransferFundsController(
            ITransferFundsWorkflow transferFundsWorkflow,
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _transferFundsWorkflow = transferFundsWorkflow;
            _exchangeClient = exchangeClient;
            _log = log;
        }

        private static List<string> RecentNonces = new List<string>();
        private const int MaxNonces = 1000;
        private static object NonceLocker = new object();

        // TODO: This should move to its own class / service
        // TODO: and it should use the database for storage.
        private static void ConsumeNonce(string nonce)
        {
            if (string.IsNullOrWhiteSpace(nonce)) { throw new ArgumentNullException(nameof(nonce)); }

            lock (NonceLocker)
            {
                if (RecentNonces.Any(queryNonce => queryNonce != null
                    && string.Equals(queryNonce.Trim(), nonce.Trim(), StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new ApplicationException($"Nonce {nonce} has already been used.");
                }

                RecentNonces.Add(nonce);
                if (RecentNonces.Count > MaxNonces)
                {
                    RecentNonces.RemoveAt(0);
                }
            }
        }


        public class TransferFundsServiceModel
        {
            public string Symbol { get; set; }
            public bool shouldTransferAll { get; set; }
            public decimal? Quantity { get; set; }
            public string Source { get; set; }
            public string Destination { get; set; }
            public string Nonce { get; set; }
        }

        private static object TransferFundsLocker = new object();

        [HttpPost]
        [Route("api/transfer-funds")]
        public HttpResponseMessage TransferFunds(TransferFundsServiceModel serviceModel)
        {
            lock (TransferFundsLocker)
            {
                try
                {
                    if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                    if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }
                    if (string.IsNullOrWhiteSpace(serviceModel.Source)) { throw new ArgumentNullException(nameof(serviceModel.Source)); }
                    if (string.IsNullOrWhiteSpace(serviceModel.Destination)) { throw new ArgumentNullException(nameof(serviceModel.Destination)); }
                    if (serviceModel.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(serviceModel.Quantity)); }
                    if (string.IsNullOrWhiteSpace(serviceModel.Nonce)) { throw new ArgumentNullException(nameof(serviceModel.Nonce)); }

                    ConsumeNonce(serviceModel.Nonce);

                    var effectiveSource = serviceModel.Source.Trim();
                    var effectiveDestination = serviceModel.Destination.Trim();

                    if (string.Equals(effectiveSource, effectiveDestination, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new ApplicationException($"Source and destination must be different. They are both \"{effectiveSource}\".");
                    }

                    var commodityDetails = _exchangeClient.GetCommoditiyForExchange(effectiveSource, serviceModel.Symbol, null, CachePolicy.ForceRefresh);
                    if (!commodityDetails.CanonicalId.HasValue) { throw new ApplicationException("Commodity must be cannonized before withdrawing."); }
                    var commodity = CommodityRes.ById(commodityDetails.CanonicalId.Value);

                    if (commodity == null) { throw new ApplicationException($"Unable to resolve commodity from symbol \"{serviceModel.Symbol}\""); }

                    _transferFundsWorkflow.Transfer(
                        effectiveSource,
                        effectiveDestination,
                        commodity,
                        serviceModel.shouldTransferAll,
                        serviceModel.Quantity);

                    var vm = new { WasSuccessful = true };

                    return Request.CreateResponse(HttpStatusCode.OK, vm);
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            }
        }

    }
}
