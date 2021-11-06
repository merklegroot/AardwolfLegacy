using Newtonsoft.Json;
using System.Collections.Generic;

namespace coss_lib.Models
{
    public class CossWalletResponse
    {
        public class WalletPayload
        {
            public List<CossWallet> Wallets { get; set; }
        }

        public bool Success { get; set; }

        public object Payload { get; set; }

        public string PayloadText
            => Payload != null && Payload is string payloadText
                    ? payloadText
                    : null;


        public WalletPayload Wallet
        {
            get
            {
                if (Payload != null && !(Payload is string))
                {
                    return JsonConvert.DeserializeObject<WalletPayload>(Payload.ToString());
                }

                return null;
            }
        }
        // public WalletPayload Payload { get; set; }
    }
}
