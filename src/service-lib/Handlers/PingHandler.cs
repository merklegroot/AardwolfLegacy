using assembly_lib;
using service_lib.Handlers;
using System;
using System.Diagnostics;
using System.Reflection;
using trade_contracts.Messages;

namespace service_lib.Handlers
{
    public interface IPingHandler : IRequestResponseHandler<PingMessage, PongMessage> { }
    public class PingHandler : IPingHandler
    {
        private readonly string _applicationName;

        public PingHandler(string applicationName)
        {
            _applicationName = applicationName;
        }

        public PongMessage Handle(PingMessage message)
        {
            var currentProcess = Process.GetCurrentProcess();
            var versionInfo = currentProcess.MainModule.FileVersionInfo;
            var applicationVersion = versionInfo.ProductVersion.ToString();

            var assembly = Assembly.GetExecutingAssembly();
            // var applicationVersion = assembly.GetName().Version.ToString();

            var buildDate = AssemblyUtil.GetBuildDate(Assembly.GetExecutingAssembly());

            var response = new PongMessage
            {
                CorrelationId = message.CorrelationId,
                MachineName = Environment.MachineName,
                ApplicationName = _applicationName,
                ApplicationVersion = applicationVersion,
                ProcessName = currentProcess.ProcessName,
                BuildDate = buildDate
            };

            return response;
        }
    }
}
