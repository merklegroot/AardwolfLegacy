using cookie_lib;
using coss_browser_workflow_lib.Models;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace coss_browser_workflow_lib
{
    public interface ICossBrowserWorkflow
    {
        CossCookieContainer GetCossCookies();
    }

    public class CossBrowserWorkflow : ICossBrowserWorkflow
    {
        private readonly ILogRepo _log;

        public CossBrowserWorkflow(ILogRepo log)
        {
            _log = log;
        }

        private static object ChromeLocker = new object();

        public CossCookieContainer GetCossCookies()
        {
            try
            {
                List<Dictionary<string, object>> cookies;
                DateTime timeStampUtc;
                lock (ChromeLocker)
                {
                    timeStampUtc = DateTime.UtcNow;
                    cookies = CookieUtil.GetChromeCookiesForHost("coss.io");                    
                }

                var getCookiesByName = new Func<string, List<Dictionary<string, object>>>(name =>
                {
                    return cookies.Where(queryCookie =>
                        queryCookie.ContainsKey("name") && queryCookie["name"] != null && string.Equals(queryCookie["name"].ToString(), name, StringComparison.Ordinal))
                        .OrderByDescending(queryCookie => queryCookie.ContainsKey("creation_utc") ? queryCookie.ContainsKey("creation_utc").ToString() : null)
                        .ToList();
                });

                var getCookieByName = new Func<string, Dictionary<string, object>>(name =>
                {
                    return cookies.Where(queryCookie =>
                        queryCookie.ContainsKey("name") && queryCookie["name"] != null && string.Equals(queryCookie["name"].ToString(), name, StringComparison.Ordinal))
                        .OrderByDescending(queryCookie => queryCookie.ContainsKey("creation_utc") ? queryCookie["creation_utc"].ToString() : null)
                        .FirstOrDefault();
                });

                const string XsrfKey = "XSRF-TOKEN";

                var xsrfCookies = getCookiesByName(XsrfKey);
                var decryptedXsrfCookies = xsrfCookies.Select(item =>
                    {
                        return new
                        {
                            Cookie = item,
                            Decrypted = CookieUtil.DecryptCookie(item)
                        };
                    })
                    .ToList();

                var decryptCookieByName = new Func<string, string>(cookieName =>
                    CookieUtil.DecryptCookie(getCookieByName(cookieName)));

                var xsrfToken = decryptCookieByName("XSRF-TOKEN");
                var sessionToken = decryptCookieByName("coss.s");

                return new CossCookieContainer
                {
                    XsrfToken = xsrfToken,
                    SessionToken = sessionToken,
                    TimeStampUtc = timeStampUtc
                };
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }
    }
}
