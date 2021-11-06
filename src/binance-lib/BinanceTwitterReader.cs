using mongo_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using cache_lib.Models;
using trade_model;
using twitter_lib;
using twitter_model;
using cache_lib;
using config_client_lib;

namespace binance_lib
{
    public class BinanceTwitterReader : IBinanceTwitterReader
    {
        private const string DatabaseName = "binance";

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(2.5)
        };

        private static TimeSpan TweetThreshold = TimeSpan.FromSeconds(5);

        private readonly IConfigClient _configClient;
        private readonly CacheUtil _cacheUtil;

        public BinanceTwitterReader(IConfigClient configClient)
        {
            _configClient = configClient;

            _cacheUtil = new CacheUtil();
        }

        public List<CommodityListing> GetBinanceListingTweets()
        {
            // "Text": "#Binance Lists #SingularityNET ( $AGI )\nhttps://t.co/Lu2CLVHfo6 https://t.co/RgdK2VX7JZ",
            var allBinanceTweets = GetBinanceTweets();
            var listingTweets = allBinanceTweets.Where(item => item.Text.ToUpper().StartsWith("#Binance Lists".ToUpper()))
                .ToList();

            return listingTweets.Select(item => new CommodityListing
            {
                CommoditySymbol = GetCommoditySymbol(item.Text),
                CommodityName = GetCommodityName(item.Text),
                TimeStampUtc = item.TimeStampUtc,
                TweetText = item.Text,
                Links = GetLinks(item.Text)
            }).ToList();
        }

        private List<Tweet> GetBinanceTweets()
        {
            var apiKey = _configClient.GetTwitterApiKey();

            var retriever = new Func<string>(() =>
                JsonConvert.SerializeObject(new TwitterClient(apiKey.Key, apiKey.Secret).SearchUserTweets("binance"))
            );

            var context = new MongoCollectionContext(_configClient.GetConnectionString(), DatabaseName, "binance--twitter");
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, TweetThreshold, CachePolicy.AllowCache, validator);
            return JsonConvert.DeserializeObject<List<Tweet>>(cacheResult.Contents);
        }

        private List<string> GetLinks(string tweetText)
        {
            const string Pattern = "(http|ftp|https)://([\\w_-]+(?:(?:\\.[\\w_-]+)+))([\\w.,@?^=%&:/~+#-]*[\\w@?^=%&/~+#-])?";
            var links = new List<string>();
            foreach (Match match in Regex.Matches(tweetText, Pattern))
            {
                links.Add(match.Value);
            }

            return links;
        }

        private string GetCommoditySymbol(string tweetText)
        {
            return GetBetweenParentheses(tweetText).Trim().Substring(1);
        }

        private string GetCommodityName(string tweetText)
        {
            var name = GetBetweenIndicators(tweetText, "#Binance Lists", "(").Trim();
            if (name.StartsWith("#")) { name = name.Substring(1); }

            return name;
        }

        private string GetBetweenIndicators(string text, string startIndicator, string stopIndicator)
        {
            if (text == null) { return null; }
            var openPos = text.IndexOf(startIndicator);
            if (openPos < 0) { return null; }
            var closePos = text.IndexOf(stopIndicator, openPos + startIndicator.Length);
            if (closePos < 0) { return null; }

            return text.Substring(openPos + startIndicator.Length, closePos - openPos - startIndicator.Length);
        }

        private string GetBetweenParentheses(string text)
        {
            return GetBetweenIndicators(text, "(", ")");
        }
    }
}
