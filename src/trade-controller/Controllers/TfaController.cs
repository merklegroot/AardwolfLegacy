using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class TfaController : BaseController
    {
        public class TfaViewModel
        {
            public string Tfa { get; set; }
        }

        [HttpPost]
        [Route("api/get-tfa")]
        public HttpResponseMessage GetTfa()
        {
            var contents = _webUtil.Get("http://localhost/tfa/api/tfa");
            if (contents != null) { contents = contents.Replace("\"", string.Empty).Trim(); }
            var vm = new TfaViewModel { Tfa = contents };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
