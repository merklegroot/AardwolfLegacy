using System;

namespace service_lib.Exceptions
{
    public class HandlerException : Exception
    {
        public string FailureReason { get; set; }

        public HandlerException() : this(null) { }

        public HandlerException(string failureReason)
        {
            FailureReason = failureReason;
        }        
    }
}
