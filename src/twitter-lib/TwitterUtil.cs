using dump_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using trade_contracts;

namespace twitter_lib
{
    public class TwitterUtil
    {
        public void More()
        {
            var apiKey = GetApiKey();
            var twitter = new Twitter();
            twitter.OAuthConsumerKey = apiKey.Key;
            twitter.OAuthConsumerSecret = apiKey.Secret;
            var results = twitter.GetTwitts("binance", 100).Result;
            results.Dump();
        }

        private class Twitter
        {
            public string OAuthConsumerSecret { get; set; }
            public string OAuthConsumerKey { get; set; }

            public async Task<IEnumerable<string>> GetTwitts(string userName, int count, string accessToken = null)
            {
                if (accessToken == null) { accessToken = await GetAccessToken(); }

                var requestUserTimeline = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, string.Format("https://api.twitter.com/1.1/statuses/user_timeline.json?count={0}&screen_name={1}&trim_user=1&exclude_replies=1", count, userName));
                requestUserTimeline.Headers.Add("Authorization", "Bearer " + accessToken);
                var httpClient = new HttpClient();
                var responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);
                var json = JsonConvert.DeserializeObject<object>(await responseUserTimeLine.Content.ReadAsStringAsync());
                var enumerableTwitts = (json as IEnumerable<dynamic>);

                if (enumerableTwitts == null)
                {
                    return null;
                }
                return enumerableTwitts.Select(t => (string)(t["text"].ToString()));
            }

            public async Task<string> GetAccessToken()
            {
                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.twitter.com/oauth2/token ");
                var customerInfo = Convert.ToBase64String(new UTF8Encoding().GetBytes(OAuthConsumerKey + ":" + OAuthConsumerSecret));
                request.Headers.Add("Authorization", "Basic " + customerInfo);
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await httpClient.SendAsync(request);

                string jsonText = await response.Content.ReadAsStringAsync();
                var item = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);
                var result = item["access_token"];
                var resultText = result.ToString();

                return resultText;
            }
        }

        private ApiKeyContract GetApiKey()
        {
            var configUrl = "http://localhost/trade/api/get-twitter-api-key";
            var client = new HttpClient();
            var response = client.PostAsync(configUrl, null).Result;
            var contents = response.Content.ReadAsStringAsync().Result;
            var container = JsonConvert.DeserializeObject<ApiKeyContainerContract>(contents);

            return container.ApiKey;
        }
    }
}
