using coss_lib.Models;
using res_util_lib;
using System.Collections.Generic;
using System.Linq;

namespace coss_lib.Res
{
    public static class CossRes
    {
        private static string _cachedWithdrawalFeesText = null;
        private static object CachedWithdrawalFeesTextLocker = new object();
        public static string WithdrawalFeesText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_cachedWithdrawalFeesText)) { return _cachedWithdrawalFeesText; }
                lock (CachedWithdrawalFeesLocker)
                {
                    if (!string.IsNullOrWhiteSpace(_cachedWithdrawalFeesText)) { return _cachedWithdrawalFeesText; }
                    return _cachedWithdrawalFeesText = ResUtil.Get("coss-withdrawal-fees.txt", typeof(CossRes).Assembly);
                }
            }
        }

        private static Dictionary<string, decimal> _cachedWithdrawalFees = null;
        private static object CachedWithdrawalFeesLocker = new object();
        public static Dictionary<string, decimal> WithdrawalFees
        {
            get
            {
                if (_cachedWithdrawalFees != null) { return _cachedWithdrawalFees; }

                lock (CachedWithdrawalFeesLocker)
                {
                    if (_cachedWithdrawalFees != null) { return _cachedWithdrawalFees; }

                    var commodities = Commodities;
                    var fees = new Dictionary<string, decimal>();
                    foreach (var commodity in commodities)
                    {
                        if (commodity.WithdrawalFee.HasValue)
                        {
                            fees[commodity.Symbol] = commodity.WithdrawalFee.Value;
                        }
                    }

                    return _cachedWithdrawalFees = fees;
                }
            }
        }

        private static List<CossCommodity> _commoditiesCache = null;
        private static object CommoditesCacheLocker = new object();
        public static List<CossCommodity> Commodities
        {
            get
            {
                if (_commoditiesCache != null) { return _commoditiesCache; }

                lock (CommoditesCacheLocker)
                {
                    if (_commoditiesCache != null) { return _commoditiesCache; }

                    var lines = WithdrawalFeesText.Trim().Replace("\r\n", "\r").Replace("\n", "\r").Split('\r')
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();

                    var commodities = new List<CossCommodity>();
                    foreach (var line in lines)
                    {
                        var pieces = line.Split(' ').Where(piece => !string.IsNullOrWhiteSpace(piece)).ToList();
                        if (pieces.Count < 3) { continue; }

                        var symbol = pieces[pieces.Count - 1].Trim();
                        if (string.IsNullOrWhiteSpace(symbol)) { continue; }

                        var feeText = pieces[pieces.Count - 2];
                        if (string.IsNullOrWhiteSpace(feeText)) { continue; }
                        if (!decimal.TryParse(feeText, out decimal fee)) { continue; }

                        var commodityName = string.Join(" ", pieces.Take(pieces.Count - 2)).Trim();

                        commodities.Add(new CossCommodity { Symbol = symbol, WithdrawalFee = fee, Name = commodityName });
                    }

                    return _commoditiesCache = commodities;
                }
            }
        }
    }
}
