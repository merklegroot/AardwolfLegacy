
namespace kucoin_lib.Models
{
    public class KucoinNativeResponse
    {
        // {"code":"UNAUTH","msg":"Signature verification failed","success":false,"timestamp":1541722955662}
        public bool Sucess { get; set; }
        public string Code { get; set; }
        public string Msg { get; set; }
        public long TimeStamp { get; set; }        
    }
}
