using System;

namespace cryptopia_lib.Models
{
    public class CryptopiaCommodityMapItem
    {
        public long NativeId { get; set; }
        public string NativeSymbol { get; set; }
        public string NativeName { get; set; }
        public Guid CanonicalId { get; set; }
        public string CanonicalSymbol { get; set; }
        public string CanonicalName { get; set; }
    }
}
