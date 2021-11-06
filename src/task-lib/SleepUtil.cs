using System;
using System.Threading;

namespace task_lib
{
    public static class SleepUtil
    {
        public static void RestlessSleep(TimeSpan timeToSleep)
        {
            var interval = TimeSpan.FromMilliseconds(25);

            var startTime = DateTime.UtcNow;
            while((DateTime.UtcNow - startTime) < timeToSleep)
            {
                Thread.Sleep(interval);
            }
        }
    }
}
