using exchange_client_lib;
using log_lib;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using trade_model;

namespace trade_api.Controllers
{
    public class PlaceOrderController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ILogRepo _log;

        public PlaceOrderController(IExchangeClient exchangeClient, ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _log = log;
        }

        public class PlaceOrderServiceModel
        {
            public string OrderType { get; set; }
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public decimal Price { get; set; }
            public decimal Quantity { get; set; }
        }

        [HttpPost]
        [Route("api/place-order")]
        public HttpResponseMessage PlaceOrder(PlaceOrderServiceModel serviceModel)
        {
            try
            { 
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

                bool result;
                if (string.Equals(serviceModel.OrderType, "buy", StringComparison.InvariantCultureIgnoreCase))
                {
                    result = _exchangeClient.BuyLimit(serviceModel.Exchange, serviceModel.Symbol, serviceModel.BaseSymbol,
                        new QuantityAndPrice
                        {
                            Quantity = serviceModel.Quantity,
                            Price = serviceModel.Price
                        });
                }
                else if (string.Equals(serviceModel.OrderType, "sell", StringComparison.InvariantCultureIgnoreCase))
                {
                    result = _exchangeClient.SellLimit(serviceModel.Exchange, serviceModel.Symbol, serviceModel.BaseSymbol,
                        new QuantityAndPrice
                        {
                            Quantity = serviceModel.Quantity,
                            Price = serviceModel.Price
                        });
                }
                else
                {
                    throw new ApplicationException($"Invalid order type \"{serviceModel.OrderType}\".");
                }

                if (!result)
                {
                    var orderText = JsonConvert.SerializeObject(serviceModel, Formatting.Indented);
                    throw new ApplicationException($"Failed to place order.{Environment.NewLine}{orderText}");
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }
    }
}