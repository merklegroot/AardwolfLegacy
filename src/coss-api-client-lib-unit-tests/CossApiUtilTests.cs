using System;
using coss_api_client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace coss_api_client_lib_unit_tests
{
    [TestClass]
    public class CossApiUtilTests
    {
        [TestMethod]
        public void Coss_api_util__generate_nonce()
        {
            var nonce = CossApiUtil.GenerateNonce();
            Console.WriteLine(nonce);
        }
    }
}
