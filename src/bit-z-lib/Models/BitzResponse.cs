using Newtonsoft.Json;

namespace bit_z_lib
{
    public class BitzResponse<T>
    {
	    //"status": 200,
        [JsonProperty("status")]
        public int Status { get; set; }

        //"msg": "",
        [JsonProperty("msg")]
        public string Msg { get; set; }

        //"data":
        [JsonProperty("data")]
        public T Data { get; set; }

        //"time": 1538671330,
        [JsonProperty("time")]
        public long Time { get; set; }

        //"microtime": "0.30154600 1538671330",
        [JsonProperty("microtime")]
        public string MicroTime { get; set; }

        //"source": "api"
        [JsonProperty("source")]
        public string Source { get; set; }
     }
}
