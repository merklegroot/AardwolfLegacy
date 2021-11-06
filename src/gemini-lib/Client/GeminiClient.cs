using web_util;

namespace gemini_lib.Client
{
    public interface IGeminiClient
    {
        string GetSymbols();
        string GetOrderBook(string symbol, string baseSymbol);
    }

    public class GeminiClient : IGeminiClient
    {
        private readonly IWebUtil _webUtil;

        public GeminiClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetSymbols()
        {
            const string Url = "https://api.gemini.com/v1/symbols";
            return _webUtil.Get(Url);
        }

        public string GetOrderBook(string symbol, string baseSymbol)
        {
            var combo = $"{symbol.ToLower()}{baseSymbol.ToLower()}";
            var url = $"https://api.gemini.com/v1/book/{combo}";

            return _webUtil.Get(url);
        }
    }
}
