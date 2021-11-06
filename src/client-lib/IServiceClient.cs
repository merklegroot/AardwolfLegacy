using client_lib.Models;
using System;

namespace client_lib
{
    public interface IServiceClient
    {
        void OverrideQueue(string queue);
        void OverrideTimeout(TimeSpan timeout);
        void UseDefaultConfigKey();
        void OverrideConfigKey(string configKey);

        PongInfo Ping();
    }
}
