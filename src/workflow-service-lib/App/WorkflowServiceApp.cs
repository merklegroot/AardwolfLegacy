using System.Collections.Generic;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using trade_constants;
using workflow_service_lib.Handlers;

namespace workflow_service_lib.App
{
    public interface IWorkflowServiceApp : IServiceApp
    {
        /// <summary>
        /// For debugging
        /// </summary>
        void SetSynchronousWorkflow(bool shouldEnable);
    }

    public class WorkflowServiceApp :
        ServiceApp, IWorkflowServiceApp
    {
        private readonly IWorkflowHandler _workflowHandler;

        public WorkflowServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IWorkflowHandler workflowHandler,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _workflowHandler = workflowHandler;
        }

        public override string ApplicationName => "Workflow Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        { _workflowHandler };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.WorkflowServiceQueue;

        protected override int MaxQueueVersion => 1;

        public void SetSynchronousWorkflow(bool shouldEnable)
        {
            _workflowHandler.SetSynchronousWorkflow(shouldEnable);
        }
    }
}
