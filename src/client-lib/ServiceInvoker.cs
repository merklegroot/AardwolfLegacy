using Newtonsoft.Json;
using web_util;

namespace client_lib
{
    public class ServiceInvoker : IServiceInvoker
    {
        private const string UrlBase = "http://localhost/trade";

        private readonly IWebUtil _webUtil;

        public ServiceInvoker(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public TResponse CallApi<TResponse>(string apiMethod, object payload = null)
            where TResponse : class
        {
            var url = $"{UrlBase}/api/{apiMethod}";
            var response = payload == null ? _webUtil.Post(url, " ") : _webUtil.Post(url, payload);
            if (response == null) { return null; }

            if (typeof(TResponse) == typeof(string))
            {
                return response as TResponse;
            }

            return JsonConvert.DeserializeObject<TResponse>(response);
        }
    }
}
