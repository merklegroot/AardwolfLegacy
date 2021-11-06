using System.Collections.Generic;

namespace binance_lib.Models.Canonical
{
    public class BcWithdrawalList
    {
        public List<BcWithdrawal> List { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
