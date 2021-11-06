using System;

namespace rabbit_lib
{
    public interface IRabbitConnection : IRabbitPublisher, IDisposable
    {
        void Listen(string queueName, Action<string> consumer, bool temporary = false);

        bool IsOpen { get; }
    }
}
