using coss_browser_service_lib.Repo;
using coss_browser_workflow_lib;
using coss_browser_workflow_lib.Models;
using service_lib.Handlers;
using System;
using System.Threading;
using trade_contracts.Messages.Browser;

namespace coss_browser_service_lib.Handlers
{
    public interface ICossBrowserHandler : IHandler,
        IRequestResponseHandler<GetCossCookiesRequestMessage, GetCossCookiesResponseMessage>
    { }

    public class CossBrowserHandler : ICossBrowserHandler
    {
        private readonly ICossCookieRepo _cossCookieRepo;
        private readonly ICossBrowserWorkflow _cossBrowserWorkflow;

        public CossBrowserHandler(
            ICossBrowserWorkflow cossBrowserWorkflow,
            ICossCookieRepo cossCookieRepo)
        {
            _cossBrowserWorkflow = cossBrowserWorkflow;
            _cossCookieRepo = cossCookieRepo;
        }

        private static ManualResetEventSlim _slim = new ManualResetEventSlim(true);

        private static DateTime? _cookieStartTimeUtc = null;
        private static DateTime? _cookisEndTimeUtc = null;
        private static CossCookieContainer _cossCookies = null;

        private static TimeSpan CacheNoMatterWhatLimit = TimeSpan.FromSeconds(5);
        private static TimeSpan CacheIfWeHaveValuesLimit = TimeSpan.FromSeconds(10);
        private static TimeSpan LockLimit = TimeSpan.FromSeconds(2.5);

        public GetCossCookiesResponseMessage Handle(GetCossCookiesRequestMessage message)
        {
            var cookieContainer = _cossCookieRepo.Get();
            return new GetCossCookiesResponseMessage
            {
                Payload = new GetCossCookiesResponseMessage.GetCookiesResponseMessagePayload
                {
                    SessionToken = cookieContainer?.SessionToken,
                    XsrfToken = cookieContainer?.XsrfToken,
                    TimeStampUtc = cookieContainer?.TimeStampUtc
                }
            };
        }
    }
}
