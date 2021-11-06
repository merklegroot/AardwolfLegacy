using assembly_lib;
using bitz_browser_lib;
using log_lib;
using Newtonsoft.Json;
using rabbit_lib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using trade_constants;
using trade_contracts;

namespace bitz_browser_con
{
    public class App : IDisposable
    {
        public string ApplicationName { get { return "Bit-Z Browser Agent"; } }
        private IRabbitConnection _rabbit;

        private readonly ILogRepo _log;

        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly IBitzBrowserUtil _browserUtil;

        public App(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IBitzBrowserUtil browserUtil,
            ILogRepo log)
        {
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _browserUtil = browserUtil;
            _log = log;
        }

        private bool _keepRunning = true;

        public void Run()
        {
            _browserUtil.UpdateFunds();
        }

        public void RunOld()
        {
            _rabbit = _rabbitConnectionFactory.Connect();
            _rabbit.Listen(TradeRabbitConstants.Queues.BitzBrowserAgentQueue, OnMessageReceived);

            Console.WriteLine($"{ApplicationName} running and connected.");
            while (_keepRunning)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) { return; }
                var lines = message.Replace("\r\n", "\r").Replace("\n", "\r").Trim().Split('\r');
                if (lines == null || !lines.Any()) { return; }

                _log.Info($"Received:{Environment.NewLine}{message}");

                var messageTypeText = lines.First();
                var messagePayload = string.Join(Environment.NewLine, lines.Skip(1));

                if (string.Equals(messageTypeText, typeof(GetStatusRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<GetStatusRequestMessage>(messagePayload);
                    Handle(contract);
                    return;
                }

                if (string.Equals(messageTypeText, typeof(UpdateFundsRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<UpdateFundsRequestMessage>(messagePayload);
                    Handle(contract);
                    return;
                }

                Console.WriteLine("Didn't recognize the message type.");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to handle message.");
                _log.Error(exception);
            }
        }

        private void Handle(GetStatusRequestMessage message)
        {
            Console.WriteLine("GetStatusRequestMessageHandler - Entry point");
            if (!string.IsNullOrWhiteSpace(message.ResponseQueue))
            {
                Console.WriteLine($"GetStatusRequestMessageHandler - sending response to {message.ResponseQueue}");
                _rabbit.PublishContract(message.ResponseQueue, new GetStatusResponseMessage
                {
                    StatusText = "All is well",
                    ApplicationName = ApplicationName,
                    MachineName = Environment.MachineName,
                    ProcessStartTime = Process.GetCurrentProcess().StartTime,
                    BuildDate = AssemblyUtil.GetBuildDate(Assembly.GetExecutingAssembly()),
                    CorrelationId = message.CorrelationId
                });
            }
            else
            {
                Console.WriteLine("GetStatusRequestMessageHandler - Response queue not specified.");
            }
        }

        private static object UpdateFundsLocker = new object();
        private static bool _isUpdatingFunds = false;
        private void Handle(UpdateFundsRequestMessage message)
        {
            if (_isUpdatingFunds)
            {
                Console.WriteLine("Already updating funds.");
                return;
            }

            lock (UpdateFundsLocker)
            {
                if (_isUpdatingFunds)
                {
                    Console.WriteLine("Already updating funds.");
                    return;
                }

                try
                {
                    _browserUtil.UpdateFunds();
                }
                finally
                {
                    _isUpdatingFunds = false;
                }
            }
        }

        public void Dispose()
        {
            if (_rabbit != null) { _rabbit.Dispose(); }
        }

        public class ContractContainer
        {
            public string Type { get; set; }
            public object Data { get; set; }

            public ContractContainer() { }

            public static ContractContainer Create<T>(T data)
            {
                return new ContractContainer { Type = data.GetType().FullName, Data = data };
            }

            public byte[] GetBytes()
            {
                return Encoding.Default.GetBytes(JsonConvert.SerializeObject(this));
            }
        }
    }
}
