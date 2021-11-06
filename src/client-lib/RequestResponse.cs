using Newtonsoft.Json;
using rabbit_lib;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using trade_contracts.Messages;

namespace client_lib
{
    public class RequestResponse : IRequestResponse
    {
        private static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
        private TimeSpan? _overriddenTimeout = null;

        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;

        public RequestResponse(IRabbitConnectionFactory rabbitConnectionFactory)
        {
            _rabbitConnectionFactory = rabbitConnectionFactory;
        }

        private TimeSpan Timeout => _overriddenTimeout ?? DefaultTimeout;

        public void OverrideTimeout(TimeSpan timeout)
        {
            _overriddenTimeout = timeout;
        }

        public TResponse Execute<TRequest, TResponse>(TRequest requestMessage, string destinationQueue)
            where TRequest : RequestMessage
            where TResponse : IResponseMessage
        {
            return Execute<TRequest, TResponse>(requestMessage, destinationQueue, null);
        }

        public TResponse Execute<TRequest, TResponse>(TRequest requestMessage, string destinationQueue, TimeSpan? timeout)
            where TRequest : RequestMessage
            where TResponse : IResponseMessage
        {
            if (requestMessage == null) { throw new ArgumentNullException(nameof(requestMessage)); }
            if (requestMessage.CorrelationId == default(Guid)) { requestMessage.CorrelationId = Guid.NewGuid(); }
            requestMessage.ResponseQueue = $"{requestMessage.GetType().Name}_{Guid.NewGuid().ToString()}";

            string response = null;
            using (var rabbit = _rabbitConnectionFactory.Connect())
            {
                var slim = new ManualResetEvent(false);

                rabbit.Listen(requestMessage.ResponseQueue, resp =>
                {
                    response = resp;
                    slim.Set();
                }, true);

                rabbit.PublishContract(destinationQueue, requestMessage, TimeSpan.FromMinutes(5));

                if (!slim.WaitOne(timeout ?? Timeout))
                {
                    throw new ApplicationException("No response.");
                }
            }

            var parsed = ParseMessage(response);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.MessageContents)) { return default(TResponse); }

            var responseMessage = JsonConvert.DeserializeObject<TResponse>(parsed.MessageContents);

            if (responseMessage == null) { throw new ApplicationException($"Received a null response when sending a {typeof(TRequest).Name}"); }
            if (!responseMessage.WasSuccessful)
            {
                var errorBuilder = new StringBuilder()
                    .AppendLine($"Received a failure response when sending a {typeof(TRequest).Name}");

                if (!string.IsNullOrWhiteSpace(responseMessage.FailureReason))
                {
                    errorBuilder.AppendLine(responseMessage.FailureReason.Trim());
                }

                throw new ApplicationException(errorBuilder.ToString());
            }

            return responseMessage;
        }

        private class MessageTypeAndContents
        {
            public string MessageType { get; set; }
            public string MessageContents { get; set; }
        }

        private MessageTypeAndContents ParseMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) { return null; }
            var messageLines = raw.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r').Where(queryLine => !string.IsNullOrWhiteSpace(queryLine)).ToList();
            if (messageLines.Count <= 0) { return null; }

            var contractName = messageLines[0].Trim();
            var remainingText = string.Join(Environment.NewLine, messageLines.Skip(1));

            return new MessageTypeAndContents { MessageType = contractName, MessageContents = remainingText };
        }

        public void OverrideConfigKey(string configKey)
        {
            _rabbitConnectionFactory.OverrideConfigKey(configKey);
        }

        public void UseDefaultConfigKey()
        {
            _rabbitConnectionFactory.UseDefaultConfigKey();
        }
    }
}
