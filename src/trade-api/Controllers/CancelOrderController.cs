using exchange_client_lib;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_api.Controllers
{
    public class CancelOrderController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;
        public CancelOrderController(IExchangeClient exchangeClient)
        {
            _exchangeClient = exchangeClient;
        }

        public class CancelOrderServiceModel
        {
            public string Exchange { get; set; }
            public string OrderId { get; set; }
        }

        [HttpPost]
        [Route("api/cancel-order")]
        public HttpResponseMessage CancelOrder(CancelOrderServiceModel serviceModel)
        {
            _exchangeClient.CancelOrder(serviceModel.Exchange, serviceModel.OrderId);

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}