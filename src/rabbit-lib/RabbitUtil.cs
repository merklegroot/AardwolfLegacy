using Newtonsoft.Json;
using rabbit_lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace rabbit_lib
{
    public static class RabbitUtil
    {
        // http://localhost:15672/api

        public static int GetConsumerCount(RabbitConnectionContext context, string queueName)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }
            if (string.IsNullOrWhiteSpace(queueName)) { throw new ArgumentNullException(nameof(queueName)); }

            var queues = GetQueues(context);
            if (queues == null || !queues.Any()) { return 0; }

            var matchingQueue = queues.FirstOrDefault(item => string.Equals(queueName, item.name, StringComparison.Ordinal));
            if (matchingQueue == null) { return 0; }

            return matchingQueue.consumers;
        }

        public static List<RabbitApiQueueInfo> GetQueues(RabbitConnectionContext context)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            var contents = GetQueuesText(context);
            var infos = JsonConvert.DeserializeObject<List<RabbitApiQueueInfo>>(contents);

            return infos;
        }

        private static object RabbitApiLocker = new object();
        private static string GetQueuesText(RabbitConnectionContext context)
        {
            lock (RabbitApiLocker)
            {
                if (context == null) { throw new ArgumentNullException(nameof(context)); }

                const string DefaultUserName = "guest";
                const string DefaultPassword = "guest";

                var url = $"http://{context.Host}:15672/api/queues";
                var req = (HttpWebRequest)WebRequest.Create(url);

                var effectiveUserName = !string.IsNullOrWhiteSpace(context.UserName) ? context.UserName.Trim() : DefaultUserName;
                var effectivePassword = !string.IsNullOrWhiteSpace(context.UserName) ? context.Password.Trim() : DefaultPassword;

                var credentialText = $"{effectiveUserName}:{effectivePassword}";

                var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(credentialText));
                req.Headers.Add("Authorization", "Basic " + encoded);

                using (var response = req.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(responseStream);
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
