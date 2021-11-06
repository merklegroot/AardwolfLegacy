using System;
using trade_contracts.Messages;

namespace client_lib
{
    public interface IRequestResponse
    {
        TResponse Execute<TRequest, TResponse>(TRequest requestMessage, string destinationQueue)
            where TRequest : RequestMessage
            where TResponse : IResponseMessage;

        TResponse Execute<TRequest, TResponse>(TRequest requestMessage, string destinationQueue, TimeSpan? timeout)
            where TRequest : RequestMessage
            where TResponse : IResponseMessage;

        void OverrideTimeout(TimeSpan timeout);
        void OverrideConfigKey(string configKey);
        void UseDefaultConfigKey();
    }
}
