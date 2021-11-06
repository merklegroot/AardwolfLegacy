using System.IO;
using System.Net;

namespace BlocktradeExchangeLib
{
    /// <summary>
    /// World's simplest web client.
    /// TODO: Make this thing async-able.
    /// </summary>
    public class WebClient : IWebClient
    {
        public string HttpGet(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }

        public string HttpPost(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentLength = 0;

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
