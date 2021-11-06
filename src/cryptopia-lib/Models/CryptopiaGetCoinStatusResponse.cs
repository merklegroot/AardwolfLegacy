using System.Collections.Generic;

namespace cryptopia_lib.Models
{
    public class CryptopiaGetCoinStatusResponse
    {
        public long iTotalRecords { get; set; }
	    public long iTotalDisplayRecords { get; set; }
	    public int sEcho { get; set; }
        public List<CryptopiaCoinStatus> aaData { get; set; }
    }
}
