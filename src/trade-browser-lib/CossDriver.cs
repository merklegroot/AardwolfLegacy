using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using OpenQA.Selenium.Remote;
using System;
using System.Threading;
using trade_browser_lib.Models;
using trade_model;
using wait_for_it_lib;

namespace trade_browser_lib
{
    public class CossDriver : ICossDriver
    {
        private readonly RemoteWebDriver _webDriver;
        private readonly ILogRepo _log;
        private readonly IMongoCollectionContext _sessionContext;
        private readonly IWaitForIt _waitForIt;

        public CossDriver(
            RemoteWebDriver webDriver,
            IMongoCollectionContext sessionContext,
            IWaitForIt waitForIt,
            ILogRepo log)
        {
            _webDriver = webDriver;
            _sessionContext = sessionContext;
            _waitForIt = waitForIt;
            _log = log;
        }

        public bool CheckSession()
        {
            var url = "https://exchange.coss.io/api/session";
            var requestTime = DateTime.UtcNow;
            _webDriver.Navigate().GoToUrl(url);
            var responseTime = DateTime.UtcNow;

            var condition = new Func<bool>(() =>
            {
                try
                {
                    var contents = _webDriver.FindElementByTagName("pre").Text;
                    var container = new ResponseContainer
                    {
                        RequestTimeUtc = requestTime,
                        ResponseTimeUtc = responseTime,
                        Contents = contents
                    };

                    _sessionContext.GetCollection<ResponseContainer>().InsertOne(container);

                    if (string.IsNullOrWhiteSpace(contents)) { return false; }
                    var session = JsonConvert.DeserializeObject<CossResponse>(contents);
                    if (session == null) { return false; }

                    return session.Successful;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    return false;
                }
            });

            return _waitForIt.Wait(condition, TimeSpan.FromSeconds(10));

        }

        public void Login()
        {
            _webDriver.Navigate().GoToUrl(CossPage.Login);
            if (!_waitForIt.Wait(() => string.Equals((_webDriver.Title ?? string.Empty).Trim(), "coss.io", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ApplicationException("Never got to the login page.");
            }

            _log.Info("Please login.");

            var condition = new Func<bool>(() =>
            {
                try
                {
                    return _webDriver.Url.ToString().ToUpper().Contains("dashboard".ToUpper());
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    return false;
                }
            });

            while (!condition())
            {
                Thread.Sleep(250);
            }

        }
    }
}
