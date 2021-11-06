using coss_browser_service_lib.Repo;
using coss_browser_workflow_lib;
using log_lib;
using proc_worfklow_lib;
using System;
using System.Threading;

namespace coss_browser_service_lib.Workers
{
    /// <summary>
    /// At a scheduled interval
    /// Launch Chrome with Coss's profile page.
    /// Wait a short amount of time so that it can update the session and XSRF token.
    /// Then close Chrome.
    /// </summary>
    public interface IChromeProcessWorker
    {
        void Start();
        void Stop();
    }

    public class ChromeProcessWorker : IChromeProcessWorker
    {
        private readonly IChromeWorkflow _chromeWorkflow;
        private readonly ICossBrowserWorkflow _cossBrowserWorkflow;
        private readonly ICossCookieRepo _cossCookieRepo;
        private readonly ILogRepo _log;
        private bool _keepRunning = false;

        public ChromeProcessWorker(
            ICossBrowserWorkflow cossBrowserWorkflow,
            IChromeWorkflow chromeWorkflow,
            ICossCookieRepo cossCookieRepo,
            ILogRepo log)
        {
            _cossBrowserWorkflow = cossBrowserWorkflow;
            _chromeWorkflow = chromeWorkflow;
            _cossCookieRepo = cossCookieRepo;
            _log = log;
        }

        private DateTime? _lastCossLaunchEndTime = null;
        private DateTime? _lastBitzLaunchEndTime = null;

        private const string CossDashboardUrl = "https://profile.coss.io";

        private const string BitzBalancesUrl = "https://u.bitz.com/assets/index";

        // The time to wait between runs.
        private static TimeSpan LaunchInterval = TimeSpan.FromMinutes(2.5);

        // Wait a short amount of time before the initial launch.
        private static TimeSpan TimeToWaitAfterStartup = TimeSpan.FromSeconds(2.5);

        private static bool ShouldLaunchCoss = false;

        public void Start()
        {            
            var processStartTime = DateTime.UtcNow;

            _keepRunning = true;

            while (_keepRunning)
            {
                if ((!_lastCossLaunchEndTime.HasValue && DateTime.UtcNow - processStartTime >= TimeToWaitAfterStartup)
                    || DateTime.UtcNow - _lastCossLaunchEndTime >= LaunchInterval)
                {

                    if (ShouldLaunchCoss)
                    {
                        try
                        {
                            _chromeWorkflow.LaunchWaitClose(CossDashboardUrl);
                            Thread.Sleep(TimeSpan.FromSeconds(2.5));
                            var cossCookies = _cossBrowserWorkflow.GetCossCookies();
                            _cossCookieRepo.Set(cossCookies);
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to open chrome to url \"{CossDashboardUrl}\"");
                            _log.Error(exception);
                        }
                    }

                    _lastCossLaunchEndTime = DateTime.UtcNow;
                }

                if ((!_lastBitzLaunchEndTime.HasValue && DateTime.UtcNow - processStartTime >= TimeToWaitAfterStartup)
                    || DateTime.UtcNow - _lastBitzLaunchEndTime >= LaunchInterval)
                {
                    try
                    {
                        _chromeWorkflow.LaunchWaitClose(BitzBalancesUrl);
                        Thread.Sleep(TimeSpan.FromSeconds(2.5));
                        // var cossCookies = _cossBrowserWorkflow.GetCossCookies();
                        // _cossCookieRepo.Set(cossCookies);
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to open chrome to url \"{CossDashboardUrl}\"");
                        _log.Error(exception);
                    }


                    _lastBitzLaunchEndTime = DateTime.UtcNow;
                }

                Thread.Sleep(TimeSpan.FromSeconds(0.25));
            }
        }

        public void Stop()
        {
            _keepRunning = false;
        }
    }
}
