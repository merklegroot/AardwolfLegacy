//using Newtonsoft.Json;
//using rabbit_lib;
//using System;
//using System.Linq;
//using System.Threading;
//using trade_contracts;

//namespace iridium_lib
//{
//    public class RequestResponse : IRequestResponse
//    {
//        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;

//        public RequestResponse(IRabbitConnectionFactory rabbitConnectionFactory)
//        {
//            _rabbitConnectionFactory = rabbitConnectionFactory;
//        }

//        public TResponse Execute<TRequest, TResponse>(TRequest requestMessage, string destinationQueue)
//            where TRequest : MessageBase
//            where TResponse : MessageBase
//        {
//            if (requestMessage == null) { throw new ArgumentNullException(nameof(requestMessage)); }
//            if (requestMessage.CorrelationId == default(Guid)) { requestMessage.CorrelationId = Guid.NewGuid(); }
//            requestMessage.ResponseQueue = Guid.NewGuid().ToString();

//            string response = null;
//            using (var rabbit = _rabbitConnectionFactory.Connect())
//            {
//                var slim = new ManualResetEvent(false);

//                rabbit.Listen(requestMessage.ResponseQueue, resp =>
//                {
//                    response = resp;
//                    slim.Set();
//                }, true);

//                rabbit.PublishContract(destinationQueue, requestMessage, TimeSpan.FromMinutes(5));

//                if (!slim.WaitOne(TimeSpan.FromSeconds(10)))
//                {
//                    throw new ApplicationException("No response.");
//                }
//            }

//            var parsed = ParseMessage(response);
//            if (parsed == null || string.IsNullOrWhiteSpace(parsed.MessageContents)) { return null; }

//            var responseMessage = JsonConvert.DeserializeObject<TResponse>(parsed.MessageContents);

//            return responseMessage;
//        }

//        private class MessageTypeAndContents
//        {
//            public string MessageType { get; set; }
//            public string MessageContents { get; set; }
//        }

//        private MessageTypeAndContents ParseMessage(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) { return null; }
//            var messageLines = raw.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r').Where(queryLine => !string.IsNullOrWhiteSpace(queryLine)).ToList();
//            if (messageLines.Count <= 0) { return null; }

//            var contractName = messageLines[0].Trim();
//            var remainingText = string.Join(Environment.NewLine, messageLines.Skip(1));

//            return new MessageTypeAndContents { MessageType = contractName, MessageContents = remainingText };
//        }
//    }
//}
