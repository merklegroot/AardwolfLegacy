using coss_cookie_lib;
using System;

namespace coss_lib
{
    public class CossCookieClient
    {
        private readonly ICossCookieUtil _cossCookieUtil;

        public CossCookieClient(ICossCookieUtil cossCookieUtil)
        {
            _cossCookieUtil = cossCookieUtil;
        }

        public string CreateWithdrawalRaw(
            string cossWalletGuid,
            string cossTfa,
            decimal quantity,
            string destinationAddress)
        {
            throw new NotImplementedException();
        }
    }
}
