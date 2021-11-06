namespace service_lib.Handlers
{
    public interface IRequestResponseHandler : IHandler { }
    public interface IRequestResponseHandler<TRequest, TResponse> : IRequestResponseHandler
    {
        TResponse Handle(TRequest message);
    }
}
