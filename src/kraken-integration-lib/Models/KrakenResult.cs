using System.Collections.Generic;

namespace kraken_integration_lib.Models
{
    public class KrakenResult<T>
    {
        public List<string> Error { get; set; }
        public T Result { get; set; }
    }
}
