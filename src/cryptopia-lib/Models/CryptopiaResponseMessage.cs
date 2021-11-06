using System;
using System.Collections.Generic;

namespace cryptopia_lib.Models
{
    public class CryptopiaResponseMessage<T>
    {
        public DateTime? AsOf { get; set; }

        public bool Success { get; set; }
        public string Message { get; set; }

        public T Data { get; set; }
    }
}
