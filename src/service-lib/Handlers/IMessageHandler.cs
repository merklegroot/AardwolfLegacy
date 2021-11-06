using rabbit_lib;

namespace service_lib.Handlers
{
    public interface IMessageHandler : IHandler { }
    public interface IMessageHandler<TMessage> : IMessageHandler
    {
        void Handle(IRabbitConnection rabbit, TMessage message);
    }
}
