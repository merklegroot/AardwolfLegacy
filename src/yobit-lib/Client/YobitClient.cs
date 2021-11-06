using web_util;

namespace yobit_lib.Client
{
    public interface IYobitClient
    {
        string GetDepth(string nativeSymbol, string nativeBaseSymbol);
    }

    public class YobitClient : IYobitClient
    {
        private readonly IWebUtil _webUtil;

        public YobitClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetDepth(string nativeSymbol, string nativeBaseSymbol)
        {
            var effectiveNativeSymbol = nativeSymbol.Trim().ToUpper();
            var effectiveNativeBaseSymbol = nativeBaseSymbol.Trim().ToUpper();

            var tradingPairForUrl = $"{effectiveNativeSymbol.ToLower()}_{effectiveNativeBaseSymbol.ToLower()}";

            var url = $"https://yobit.net/api/2/{tradingPairForUrl}/depth";

            return _webUtil.Get(url);
        }
    }
}
