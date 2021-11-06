using System;

namespace rabbit_lib
{
    public interface IRabbitPublisher
    {
        void Publish(string routingKey, string message, TimeSpan? expiration = null);
        void PublishContract<T>(string routingKey, T message, TimeSpan? expiration = null);
    }
}
