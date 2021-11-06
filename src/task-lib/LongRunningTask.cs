using System;
using System.Threading.Tasks;

namespace task_lib
{
    public static class LongRunningTask
    {
        public static Task Run(Action method)
        {
            var task = new Task(method, TaskCreationOptions.LongRunning);
            task.Start();

            return task;
        }

        public static Task<T> Run<T>(Func<T> method)
        {
            var task = new Task<T>(method, TaskCreationOptions.LongRunning);
            task.Start();

            return task;
        }
    }
}
