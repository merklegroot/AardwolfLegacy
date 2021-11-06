using assembly_lib;
using browser_automation_client_lib;
using client_lib;
using config_client_lib;
using coss_browser_service_client;
using cryptocompare_client_lib;
using exchange_client_lib;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using rabbit_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using trade_constants;
using trade_contracts;
using trade_node_integration;
using web_util;
using workflow_client_lib;

namespace trade_api.Controllers
{
    public class StatusController : ApiController
    {
        private readonly IPingClient _pingClient;

        private readonly IConfigClient _configClient;
        private readonly ICossBrowserClient _cossBrowserClient;
        
        private readonly IWebUtil _webUtil;
        private readonly ITradeNodeUtil _tradeNodeUtil;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;


        public StatusController(
            IPingClient pingClient,

            IConfigClient configClient,
            ICossBrowserClient cossBrowserClient,
            
            IWebUtil webUtil,
            ITradeNodeUtil tradeNodeUtil,
            IRabbitConnectionFactory rabbitConnectionFactory)
        {
            _pingClient = pingClient;
            _configClient = configClient;
            _cossBrowserClient = cossBrowserClient;

            _webUtil = webUtil;
            _tradeNodeUtil = tradeNodeUtil;
            _rabbitConnectionFactory = rabbitConnectionFactory;
        }

        private static readonly RabbitConnectionContext _rabbitConnectionContext = new RabbitConnectionContext
        {
            Host = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        internal class ServiceMap
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public IServiceClient Client { get; set; }
        }

        public class PingServiceServiceModel
        {
            public string Service { get; set; }
        }

        [HttpPost]
        [Route("api/ping-service")]
        public HttpResponseMessage PingService(PingServiceServiceModel serviceModel)
        {
            var pingResult = _pingClient.Ping(serviceModel.Service);            
            return Request.CreateResponse(pingResult);
        }

        [HttpPost]
        [Route("api/get-services")]
        public HttpResponseMessage GetServices()
        {
            var vm = ServiceRes.All.Select(item => new { id = item.Id, name = item.DisplayName }).ToList();
            return Request.CreateResponse(vm);
        }

        [HttpPost]
        [Route("api/get-server-name")]
        public HttpResponseMessage GetServerName()
        {
            return Request.CreateResponse(HttpStatusCode.OK, Environment.MachineName);
        }

        [HttpPost]
        [Route("api/get-app-server-build-date")]
        public HttpResponseMessage GetAppServerBuildDate()
        {
            return Request.CreateResponse(HttpStatusCode.OK, AssemblyUtil.GetBuildDate(Assembly.GetExecutingAssembly()));
        }

        [HttpPost]
        [Route("api/get-database-status")]
        public HttpResponseMessage GetDatabaseStatus()
        {
            bool wasSuccessful = false;
            string displayText = "not attempted.";

            try
            {
                var connectionString = _configClient.GetConnectionString();

                var testContext = new MongoDatabaseContext(connectionString, "test");
                var collectionContext = new MongoCollectionContext(testContext, "testCollection");
                var collection = collectionContext.GetCollection<BsonDocument>();
                var doc = collection.AsQueryable().FirstOrDefault();

                wasSuccessful = true;
                displayText = "Connection Successful!";
            }
            catch(Exception exception)
            {
                wasSuccessful = false;
                displayText = exception.Message;
            }

            var vm = new { wasSuccessful = wasSuccessful, displayText = displayText };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-ccxt-service-status")]
        public HttpResponseMessage GetCcxtServiceStatus()
        {
            var result = _tradeNodeUtil.IsOnline();
            var displayText = result ? "Online" : "Offline";

            var vm = new { wasSuccessful = result, displayText = displayText };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-etherscan-service-status")]
        public HttpResponseMessage GetEtherscanAgentStatus()
        {
            return GetAgentStatus(TradeRabbitConstants.Queues.EtherscanAgentQueue);
        }

        [HttpPost]
        [Route("api/get-coss-service-status")]
        public HttpResponseMessage GetCossAgentStatus()
        {
            return GetComplexStatus(TradeRabbitConstants.Queues.CossAgentQueue);
        }

        [HttpPost]
        [Route("api/get-kucoin-service-status")]
        public HttpResponseMessage GetKucoinAgentStatus()
        {
            return GetComplexStatus(TradeRabbitConstants.Queues.KucoinAgentQueue);
        }

        [HttpPost]
        [Route("api/get-idex-service-status")]
        public HttpResponseMessage GetIdexAgentStatus()
        {
            return GetAgentStatus(TradeRabbitConstants.Queues.IdexAgentQueue);
        }

        [HttpPost]
        [Route("api/get-bitz-service-status")]
        public HttpResponseMessage GetBitzAgentStatus()
        {
            return GetAgentStatus(TradeRabbitConstants.Queues.BitzBrowserAgentQueue);
        }

        [HttpPost]
        [Route("api/get-mew-service-status")]
        public HttpResponseMessage GetMewAgentStatus()
        {
            return GetAgentStatus(TradeRabbitConstants.Queues.MewAgentQueue);
        }

        [HttpPost]
        [Route("api/status-coss-cookie-test")]
        public HttpResponseMessage StatusCossCookieTest()
        {
            var cookies = _cossBrowserClient.GetCookies();
            return Request.CreateResponse(cookies);
        }

        private HttpResponseMessage GetAgentStatus(string agentQueue)
        {
            var consumerCount = RabbitUtil.GetConsumerCount(_rabbitConnectionContext, agentQueue);

            var wasSuccessful = consumerCount == 1;
            var displayText = $"Consumers: {consumerCount}";

            var vm = new { wasSuccessful = wasSuccessful, displayText = displayText };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        private HttpResponseMessage GetComplexStatus(string target)
        {
            var timeout = TimeSpan.FromSeconds(5);

            var consumerCount = RabbitUtil.GetConsumerCount(_rabbitConnectionContext, target);

            var slim = new ManualResetEventSlim();
            var req = new GetStatusRequestMessage();

            GetStatusResponseMessage response = null;
            if (consumerCount > 0)
            {
                using (var conn = _rabbitConnectionFactory.Connect())
                {
                    Console.WriteLine($"Listening to response queue {req.ResponseQueue}");
                    conn.Listen(req.ResponseQueue, resp =>
                    {
                        var pos = resp.IndexOf('\r');
                        if (pos >= 0)
                        {
                            response = JsonConvert.DeserializeObject<GetStatusResponseMessage>(resp.Substring(pos));
                        }

                        slim.Set();
                    }, true);

                    conn.PublishContract(target, req, timeout);
                    slim.Wait(timeout);
                }
            }

            var displayText = new List<string>();
            displayText.Add($"Consumers: {consumerCount}");

            if (response != null)
            {
                displayText.Add($"Build Date: {response?.BuildDate}");
                displayText.Add($"Process Start Time: {response?.ProcessStartTime}");
                displayText.Add($"Status Text: {response?.StatusText}");
            }

            var wasSuccessful = consumerCount > 0;
            if ((response?.StatusText ?? string.Empty).ToUpper().Contains("Not logged in".ToUpper()))
            {
                wasSuccessful = false;
            }

            var vm = new
            {
                wasSuccessful = consumerCount > 0,
                displayText = string.Join("; ", displayText)
            };

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
