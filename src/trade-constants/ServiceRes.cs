using System;
using System.Collections.Generic;

namespace trade_constants
{
    public static class ServiceRes
    {
        public static List<ServiceDef> All = new List<ServiceDef>
        {
            Config,
            Exchange,
            Workflow,
            CryptoCompare,
            CossBrowser,
            BrowserAutomation,
            CossArb,
            QryptosArb
        };

        private static Dictionary<string, ServiceDef> _dictionary = null;
        public static Dictionary<string, ServiceDef> Dictionary
        {
            get
            {
                if (_dictionary != null) { return _dictionary; }

                var dict = new Dictionary<string, ServiceDef>(StringComparer.InvariantCultureIgnoreCase);
                foreach(var item in All)
                {
                    dict[item.Id] = item;
                }

                return _dictionary = dict;
            }
        }

        public static ServiceDef Config => new ServiceDef
        {
            Id = "config",
            Queue = "config-service",
            DisplayName = "Config Service"
        };

        public static ServiceDef Exchange => new ServiceDef
        {
            Id = "exchange",
            Queue = "exchange-service",
            DisplayName = "Exchange Service"
        };

        public static ServiceDef Workflow => new ServiceDef
        {
            Id = "workflow",
            Queue = "workflow-service",
            DisplayName = "Workfow Service"
        };

        public static ServiceDef CryptoCompare => new ServiceDef
        {
            Id = "cryptocompare",
            Queue = "cryptocompare-service",
            DisplayName = "CryptoCompare Service"
        };

        public static ServiceDef CossBrowser => new ServiceDef
        {
            Id = "cossbrowser",
            Queue = "coss-browser-service",
            DisplayName = "Coss Browser Service"
        };

        public static ServiceDef BrowserAutomation => new ServiceDef
        {
            Id = "browserautomation",
            Queue = "browser-automation-service",
            DisplayName = "Browser Automation Service"
        };

        public static ServiceDef CossArb => new ServiceDef
        {
            Id = "cossarb",
            Queue = "coss-arb-service",
            DisplayName = "Coss Arb Service"
        };

        public static ServiceDef QryptosArb => new ServiceDef
        {
            Id = "qryptosarb",
            Queue = "qryptos-arb-service",
            DisplayName = "Qryptos Arb Service"
        };
    }
}
