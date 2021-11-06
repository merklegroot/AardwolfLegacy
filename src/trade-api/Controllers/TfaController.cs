using System.Net;
using System.Net.Http;
using System.Web.Http;
using tfa_lib;
using web_util;

namespace trade_api.Controllers
{
    public class TfaController : ApiController
    {
        private readonly ITfaUtil _tfaUtil;

        public TfaController(ITfaUtil tfaUtil)
        {
            _tfaUtil = tfaUtil;
        }

        public class TfaViewModel
        {
            public string Tfa { get; set; }
        }

        [HttpPost]
        [Route("api/get-tfa")]
        public HttpResponseMessage GetTfa()
        {
            var vm = new TfaViewModel { Tfa = _tfaUtil.GetCossTfa() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
