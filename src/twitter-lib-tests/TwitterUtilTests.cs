using Microsoft.VisualStudio.TestTools.UnitTesting;
using twitter_lib;

namespace twitter_lib_tests
{
    [TestClass]
    public class TwitterUtilTests
    {
        private TwitterUtil _twitterUtil;

        [TestInitialize]
        public void Setup()
        {
            _twitterUtil = new TwitterUtil();
        }

        [TestMethod]
        public void TwitterUtil_more()
        {
            _twitterUtil.More();
        }
    }
}
