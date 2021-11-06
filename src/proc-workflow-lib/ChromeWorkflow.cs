using System;
using System.Diagnostics;
using task_lib;

namespace proc_worfklow_lib
{
    public interface IChromeWorkflow
    {
        void LaunchWaitClose(string url);
        bool IsChromeOpen { get; }
    }

    public class ChromeWorkflow : IChromeWorkflow
    {
        private static TimeSpan TimeToKeepChromeOpen = TimeSpan.FromSeconds(30);

        public bool IsChromeOpen { get; private set; }

        public void LaunchWaitClose(string url)
        {
            var info = new ProcessStartInfo
            {
                FileName = "chrome.exe",
                Arguments = url 
            };

            IsChromeOpen = true;
            try
            {
                using (var proc = Process.Start(info))
                {
                    SleepUtil.RestlessSleep(TimeToKeepChromeOpen);
                    proc.CloseMainWindow();
                }
            }
            finally
            {
                IsChromeOpen = false;
            }
        }
    }
}
