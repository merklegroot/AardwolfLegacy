using config_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using twitter_lib;

namespace twitter_lib_tests
{
    [TestClass]
    public class TwitterClientTests
    {
        private TwitterClient _twitterClient;

        [TestInitialize]
        public void Setup()
        {
            var config = new ConfigRepo();
            var apiKey = config.GetTwitterApiKey();
            _twitterClient = new TwitterClient(apiKey.Key, apiKey.Secret);
        }

        [TestMethod]
        public void Twitter_client__search_user_tweets()
        {
            var tweets = _twitterClient.SearchUserTweets("binance");
            tweets.Dump();
        }
    }
}
