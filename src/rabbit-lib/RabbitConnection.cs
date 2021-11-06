using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace rabbit_lib
{
    public class RabbitConnection : IRabbitConnection
    {        
        internal IConnection _connection;
        internal IModel _channel;

        internal RabbitConnection()
        {
        }

        public void Listen(string queueName, Action<string> consumer, bool temporary = false)
        {
            _channel.QueueDeclare(queue: queueName,
                         durable: false,
                         exclusive: false,
                         autoDelete: temporary,
                         arguments: null);

            var _consumer = new EventingBasicConsumer(_channel);

            _consumer.Received += (model, ea) =>
            {   
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                consumer(message);
            };

            _channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: _consumer);
        }

        public void Publish(string routingKey, string message, TimeSpan? expiration = null)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            IBasicProperties props = _channel.CreateBasicProperties();
            if (expiration.HasValue)
            {
                props.Expiration = ((int)expiration.Value.TotalMilliseconds).ToString();
            }

            _channel.BasicPublish(exchange: "",
                     routingKey: routingKey,
                     basicProperties: props,
                     body: bytes);
        }

        public void PublishContract<T>(string routingKey, T message, TimeSpan? expiration = null)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);

            var messageText = new StringBuilder()
                .AppendLine(typeof(T).FullName)
                .AppendLine(serializedMessage)
                .ToString();

            Publish(routingKey, messageText, expiration);
        }

        public void Dispose()
        {
            if (_channel != null) { _channel.Dispose(); }
            if (_connection != null) { _connection.Dispose(); }
        }

        public bool IsOpen => _connection.IsOpen;
    }
}
