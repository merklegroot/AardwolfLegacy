using System;
using System.Collections.Generic;

namespace cryptopia_lib.Models
{
    // ["619", "TenX", "PAY", "2", "None", 
    // "Asset", "7", "5747579", "OK", "", 
    // "Delisting", "Geth/v1.7.2 - PAY"]
    public class CryptopiaCoinStatus : List<string>
    {
        public long Id => GetLong(0);
        public string CommodityName => GetString(1);
        public string Symbol => GetString(2);
        public int Rating => GetInt(3);
        public string Algorithm => GetString(4);
        public string Network => GetString(5);
        public int Connections => GetInt(6);
        public string WalletStatusText => GetString(10);
        public string ListingStatusText => GetString(12);

        public CryptopiaWalletStatusEnum WalletStatus
        {
            get
            {
                var walletStatusDictionary = new Dictionary<string, CryptopiaWalletStatusEnum>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "Active", CryptopiaWalletStatusEnum.Active },
                    { "Delisting", CryptopiaWalletStatusEnum.Delisting },
                };

                return walletStatusDictionary.ContainsKey(WalletStatusText)
                    ? walletStatusDictionary[WalletStatusText]
                    : CryptopiaWalletStatusEnum.Unknown;
            }
        }

        private string GetString(int index)
        {
            return this != null && Count > index ? this[index] : null;
        }

        public long GetLong(int index)
        {
            return long.Parse(GetString(index));
        }

        public int GetInt(int index)
        {
            return int.Parse(GetString(index));
        }
    }
}
