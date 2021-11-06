using System;

namespace bitz_agent_lib.App
{
    public interface IApp : IDisposable
    {
        void Run();
    }
}
