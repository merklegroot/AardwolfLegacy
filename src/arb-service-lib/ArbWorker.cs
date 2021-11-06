using log_lib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using task_lib;

namespace arb_service_lib
{
    public interface IArbWorker
    {
        void Run();
    }

    public abstract class ArbWorker : IArbWorker
    {
        private readonly ILogRepo _log;

        public ArbWorker(ILogRepo log)
        {
            _log = log;
        }

        public virtual void Run()
        {
            var tasks = new List<Task>();
            foreach (var job in Jobs)
            {
                tasks.Add(LongRunningTask.Run(() => Forever(job)));
            }

            tasks.ForEach(task => task.Wait());
        }

        protected virtual List<Action> Jobs => new List<Action>();

        protected void Forever(Action method)
        {
            while (true)
            {
                try
                {
                    method();
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }

                Thread.Sleep(10);
            }
        }
    }
}
