using StructureMap;
using System;
using workflow_service_lib.App;

namespace workflow_service_con
{
    public class WorkflowServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null, bool useSynchronousWorkflow = false)
        {
            var container = Container.For<WorkflowServiceRegistry>();
            var service = container.GetInstance<IWorkflowServiceApp>();

            if (!string.IsNullOrWhiteSpace(overriddenQueueName))
            {
                service.OverrideQueue(overriddenQueueName);
            }

            if (useSynchronousWorkflow)
            {
                service.SetSynchronousWorkflow(useSynchronousWorkflow);
            }

            service.OnStarted += () => { OnStarted?.Invoke(); };
            try
            {
                service.Run();
            }
            finally
            {
                service.Dispose();
            }
        }
    }
}
