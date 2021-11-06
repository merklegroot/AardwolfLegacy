using log_lib.Models;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_lib;
using System.Reflection;
using System.Linq;
using log_lib;

namespace trade_api.Controllers
{
    public class LogController : ApiController
    {
        private const int MaxLogs = 1000;

        private readonly ILogRepo _logRepo;

        public LogController(ILogRepo logRepo)
        {
            _logRepo = logRepo;
        }

        [HttpPost]
        [Route("api/get-logs")]
        public HttpResponseMessage GetLogs()
        {
            // var logs = _logRepo.GetExcluding(MaxLogs);
            var logs = _logRepo.GetExcluding(MaxLogs, new List<LogLevel> { LogLevel.Verbose });
            return Request.CreateResponse(HttpStatusCode.OK, logs);
        }

        //[HttpPost]
        //[Route("api/get-error-logs")]
        //public HttpResponseMessage GetErrorLogs()
        //{
        //    var logs = _logRepo.GetErrorLogs(MaxLogs);
        //    return Request.CreateResponse(HttpStatusCode.OK, logs);
        //}

        //[HttpPost]
        //[Route("api/get-agent-logs")]
        //public HttpResponseMessage GetManageOrderLogs()
        //{
        //    var props = typeof(TradeEventType).GetProperties(BindingFlags.Static | BindingFlags.Public)
        //        .ToList();

        //    var eventTypes = props.Select(prop => (EventType)prop.GetValue(null)).ToList();


        //    var logs = _logRepo.GetForEventTypes(eventTypes, MaxLogs);
        //    return Request.CreateResponse(HttpStatusCode.OK, logs);
        //}
    }
}
