using web_util;

namespace tfa_lib
{
    public class TfaUtil : ITfaUtil
    {
        private readonly IWebUtil _webUtil;

        public TfaUtil(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetCossTfa()
        {
            return GetTfa("coss");
        }
        
        public string GetBitzTfa()
        {
            return GetTfa("bitz");
        }

        private string GetTfa(string exchange)
        {
            return _webUtil.Get($"http://localhost/tfa/api/{exchange}-tfa")
                .Replace("\"", string.Empty).Trim();
        }
    }
}
