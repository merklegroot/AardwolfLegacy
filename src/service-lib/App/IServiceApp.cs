using System;

namespace service_lib
{
    public interface IServiceApp : IDisposable
    {
        event Action OnStarted;
        void Run();

        // for testing purposes.
        void OverrideQueue(string queue);
    }
}
