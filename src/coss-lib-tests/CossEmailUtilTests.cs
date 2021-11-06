//using coss_lib;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Shouldly;
//using test_shared;
//using web_util;

//namespace coss_lib_tests
//{
//    [TestClass]
//    public class CossEmailUtilTests
//    {
//        private CossEmailUtil _emailUtil;

//        [TestInitialize]
//        public void Setup()
//        {
//            var webUtil = new WebUtil();
//            _emailUtil = new CossEmailUtil(webUtil);
//        }

//        [TestMethod]
//        public void Coss__email_link_test()
//        {
//            var link = _emailUtil.GetWithdrawalLink("STX", 208.44208548m);
//            link.Dump();
//            link.ShouldBe("https://profile.coss.io/user-confirm-withdrawal;hash=976a7124-a86f-4c8e-9623-5093a3173f0b");
//        }
//    }
//}
