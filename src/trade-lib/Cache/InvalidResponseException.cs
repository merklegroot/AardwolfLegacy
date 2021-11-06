using System;
using System.Runtime.Serialization;

namespace trade_lib.Cache
{
    public class InvalidResponseException : Exception
    {
        public InvalidResponseException() { }
        public InvalidResponseException(string message) : base(message) { }
        public InvalidResponseException(string message, Exception innerException) : base(message, innerException) { }
        public InvalidResponseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
