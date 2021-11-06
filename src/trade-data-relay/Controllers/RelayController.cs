using config_lib;
using idex_data_lib;
using idex_model;
using iridium_lib;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using trade_data_relay.ServiceModels;

namespace trade_data_relay.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class RelayController : ApiController
    {
        private readonly IIdexFrameRepo _idexFrameRepo;
       
        public RelayController()
        {
            _idexFrameRepo = new IdexFrameRepo(new ConfigClient());
        }

        private static DateTime? LastTimeDataWasTruncated = null;

        [HttpPost]
        [Route("api/collector")]
        public HttpResponseMessage CollectData(SocketDataServiceModel serviceModel)
        {
            var dataModel = new IdexFrameContainer
            {
                ClientMachine = serviceModel.ClientMachineName,
                ClientTimeStampLocal = serviceModel.ClientTimeStampLocal,
                RelayServerTimeStampUtc = DateTime.UtcNow,
                RelayServiceMachineName = Environment.MachineName,
                FrameContents = serviceModel.FrameContents
            };

            _idexFrameRepo.Insert(dataModel);

            if (!LastTimeDataWasTruncated.HasValue && LastTimeDataWasTruncated - DateTime.UtcNow > TimeSpan.FromMinutes(30))
            {
                LastTimeDataWasTruncated = DateTime.UtcNow;
                var task = new Task(() => _idexFrameRepo.TruncateOldData(), TaskCreationOptions.LongRunning);
                task.Start();
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

    }
}