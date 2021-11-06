using coss_browser_service_client;
using coss_cookie_lib.Exceptions;
using log_lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace coss_cookie_lib
{
    public interface ICossCookieUtil
    {
        string CossAuthRequest(string url);
        string CossAuthRequest(string url, string verb, string body);
    }

    public class CossCookieUtil : ICossCookieUtil
    {
        private readonly ICossBrowserClient _cossBrowserClient;
        private readonly ILogRepo _log;

        public CossCookieUtil(
            ICossBrowserClient cossBrowserClient,
            ILogRepo logRepo)
        {
            _cossBrowserClient = cossBrowserClient;
            _log = logRepo;
        }

        public string CossAuthRequest(string url)
        {
            return CossAuthRequest(url, "GET", null);
        }

        public string CossAuthRequest(string url, string verb, string body)
        {
            var cookies = _cossBrowserClient.GetCookies();
            if (cookies == null) { throw new ApplicationException("Received null cookies."); }
            if (string.IsNullOrWhiteSpace(cookies.SessionToken)) { throw new ApplicationException("Coss session token must not be null or whitespace."); }
            if (string.IsNullOrWhiteSpace(cookies.XsrfToken)) { throw new ApplicationException("Coss Xsrf token must not be null or whitespace."); }

            var cookiePieces = new List<(string Key, string Value)>();
            cookiePieces.Add(("coss.s", cookies.SessionToken));
            cookiePieces.Add(("XSRF-TOKEN", cookies.XsrfToken));

            var cookieText = string.Join(
                "; ",
                cookiePieces.Select((piece, index) => $"{piece.Key}={piece.Value}").ToList());

            // This is needed for older versions of Windows.
            // Since this is a static change, it also should likely be put into its own service
            // so as to not affect other calls.
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Headers.Add("x-xsrf-token", cookies.XsrfToken);
            req.Headers.Add("cookie", cookieText);

            if (!string.IsNullOrWhiteSpace(verb) && !string.Equals(verb, "GET", StringComparison.InvariantCultureIgnoreCase))
            {
                req.Method = verb.ToUpper();
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                using (var reqStream = req.GetRequestStream())
                {
                    req.ContentType = "application/json; charset=utf-8";

                    var postData = Encoding.Default.GetBytes(body);
                    reqStream.Write(postData, 0, postData.Length);
                }
            }

            string responseText;
            try
            {
                using (var resp = req.GetResponse())
                using (var stream = resp.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    responseText = reader.ReadToEnd();
                }
            }
            catch (WebException webException)
            {
                try
                {
                    using (var exceptionResponse = webException.Response)
                    using (var responseStream = exceptionResponse.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        var webExceptionContents = reader.ReadToEnd();
                        _log.Error(webExceptionContents);
                    }
                }
                catch { }

                throw;
            }

            const string NotLoggedInText = "{\"successful\":false,\"payload\":\"NOT_LOGGED_IN\"}";
            if (responseText != null && string.Equals(responseText.Trim(), NotLoggedInText, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new CossNotLoggedInException();
            }

            return responseText;
        }
    }
}
