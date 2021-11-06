using client_lib;
using coss_browser_client_lib;

namespace coss_browser_service_client
{
    public interface ICossBrowserClient : IServiceClient
    {
        CossCookieContainer GetCookies();
    }
}
