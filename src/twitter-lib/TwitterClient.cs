using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using twitter_model;

namespace twitter_lib
{
    public class TwitterClient
    {
        private readonly string _publicKey;
        private readonly string _privateKey;

        public TwitterClient(string publicKey, string privateKey)
        {
            _publicKey = publicKey;
            _privateKey = privateKey;
        }

        public List<Tweet> SearchUserTweets(
            string twitterUserName,
            int maxResults = 100)
        {
            var accessToken = GetAccessToken();

            var url = string.Format("https://api.twitter.com/1.1/statuses/user_timeline.json?count={0}&screen_name={1}&trim_user=1&exclude_replies=1", maxResults, twitterUserName);
            var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get, url);
            requestUserTimeline.Headers.Add("Authorization", "Bearer " + accessToken);
            var httpClient = new HttpClient();
            var response = httpClient.SendAsync(requestUserTimeline).Result;
            var responseText = response.Content.ReadAsStringAsync().Result;

            var conversionModels = JsonConvert.DeserializeObject<List<TweetConversionModel>>(responseText);

            return conversionModels.Select(item =>
                new Tweet
                {
                    Id = item.Id,
                    Text = item.Text,
                    TimeStampUtc = DateTime.ParseExact(item.CreatedAt, "ddd MMM dd HH:mm:ss +ffff yyyy", CultureInfo.InvariantCulture),
                    FavoriteCount = item.FavoriteCount,
                    IsQuoteStatus = item.IsQuoteStatus,
                    RetweetCount = item.RetweetCount,
                    Truncated = item.Truncated
                }).ToList();
        }

        private string GetAccessToken()
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth2/token ");
            var customerInfo = Convert.ToBase64String(new UTF8Encoding().GetBytes(_publicKey + ":" + _privateKey));
            request.Headers.Add("Authorization", "Basic " + customerInfo);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = httpClient.SendAsync(request);

            var responseText = response.Result.Content.ReadAsStringAsync().Result;
            var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseText);
            var result = responseDictionary["access_token"];
            var resultText = result.ToString();

            return resultText;
        }
    }
}
