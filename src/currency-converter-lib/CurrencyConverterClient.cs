using config_client_lib;
using log_lib;
using System;
using System.Collections.Generic;
using web_util;

namespace currency_converter_lib
{
    public interface ICurrencyConverterClient
    {
        string GetRawForexRate(string symbol);
    }

    public class CurrencyConverterClient : ICurrencyConverterClient
    {
        private readonly IWebUtil _webUtil;
        private readonly ILogRepo _log;

        public CurrencyConverterClient(
            IConfigClient configClient,
            IWebUtil webUtil,
            ILogRepo log)
        {
            _webUtil = webUtil;
        }



        public string GetRawForexRate(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var effectiveSymbol = symbol.Trim().ToUpper();
            var url = $"http://free.currencyconverterapi.com/api/v5/convert?q={effectiveSymbol.ToUpper()}_USD&compact=y";
            return _webUtil.Get(url);
        }
    }
}
