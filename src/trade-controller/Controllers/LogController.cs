using log_lib.Models;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_lib;
using System.Reflection;
using System.Linq;

namespace trade_web.Controllers
{
    public class LogController : BaseController
    {
        private const int MaxLogs = 100;

        [HttpGet]
        [Route("api/get-logs")]
        public HttpResponseMessage GetLogs()
        {
            var logs = _logRepo.Get(MaxLogs);
            return Request.CreateResponse(HttpStatusCode.OK, logs);
        }

        [HttpGet]
        [Route("api/get-agent-logs")]
        public HttpResponseMessage GetManageOrderLogs()
        {
            var props = typeof(TradeEventType).GetProperties(BindingFlags.Static | BindingFlags.Public)
                .ToList();

            var eventTypes = props.Select(prop => (EventType)prop.GetValue(null)).ToList();


            var logs = _logRepo.GetForEventTypes(eventTypes, MaxLogs);
            return Request.CreateResponse(HttpStatusCode.OK, logs);
        }
    }
}
