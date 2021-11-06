using assembly_lib;
using browser_lib;
using log_lib;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using rabbit_lib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using trade_constants;
using trade_contracts;
using wait_for_it_lib;

namespace kucoin_agent
{
    public class App : IDisposable
    {
        public const string ApplicationName = "Kucoin Agent";

        private readonly IBrowserUtil _browserUtil;
        private readonly IWaitForIt _waitForIt;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly ILogRepo _log;

        private bool _keepRunning;

        private IRabbitConnection _rabbit;

        public App(
            IBrowserUtil browerUtil,
            IWaitForIt waitForIt,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
        {
            _browserUtil = browerUtil;
            _waitForIt = waitForIt;
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _log = log;
        }

        public void Run()
        {
            _keepRunning = true;

            _rabbit = _rabbitConnectionFactory.Connect();
            _rabbit.Listen(TradeRabbitConstants.Queues.KucoinAgentQueue, OnMessageReceived);

            Info($"{ApplicationName} running and connected.");
            while (_keepRunning)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
        }

        private static object OpenUrlLocker = new object();

        private void OnMessageReceived(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) { return; }
                var lines = message.Replace("\r\n", "\r").Replace("\n", "\r").Trim().Split('\r');
                if (lines == null || !lines.Any()) { return; }

                Info($"Received:{Environment.NewLine}{message}");

                var messageTypeText = lines.First();
                var messagePayload = string.Join(Environment.NewLine, lines.Skip(1));

                if (string.Equals(messageTypeText, typeof(ConfirmWithdrawalLinkRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<ConfirmWithdrawalLinkRequestMessage>(messagePayload);
                    ConfirmEmailLinkHandler(contract);
                    return;
                }

                if (string.Equals(messageTypeText, typeof(GetStatusRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<GetStatusRequestMessage>(messagePayload);
                    GetStatusRequestMessageHandler(contract);
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

        private enum EmailLinkResult
        {
            Unknown,
            Success,
            Failure
        }

        private void ConfirmEmailLinkHandler(ConfirmWithdrawalLinkRequestMessage message)
        {
            Console.WriteLine("Handler...");
            try
            {
                Console.WriteLine("Open the url!");
                EmailLinkResult linkResult = EmailLinkResult.Unknown;
                using (var driver = new ChromeDriver())
                {
                    driver.Navigate().GoToUrl(message.Url);                    
                    _waitForIt.Wait(() =>
                    {
                        var upperSource = driver.PageSource.ToUpper();
                        if (upperSource.Contains("The application has been submitted".ToUpper()))
                        {
                            linkResult = EmailLinkResult.Success;
                        }
                        else if (driver.PageSource.ToUpper().Contains("The link has failed".ToUpper()))
                        {
                            linkResult = EmailLinkResult.Failure;
                        }

                        return linkResult != EmailLinkResult.Unknown;
                    }, TimeSpan.FromSeconds(15));                    
                }

                Info($"Kucoin Link result: {linkResult}");

                if (!string.IsNullOrWhiteSpace(message.ResponseQueue))
                {
                    _rabbit.PublishContract(message.ResponseQueue, new ConfirmWithdrawalLinkResponseMessage
                    {
                        WasSuccessful = linkResult == EmailLinkResult.Success,
                        CorrelationId = message.CorrelationId
                    });
                }
            }
            catch(Exception exception)
            {
                if (!string.IsNullOrWhiteSpace(message.ResponseQueue))
                {
                    _rabbit.PublishContract(message.ResponseQueue, new ConfirmWithdrawalLinkResponseMessage
                    {
                        WasSuccessful = false,
                        FailureReason = exception.Message,
                        CorrelationId = message.CorrelationId
                    });
                }

                throw;
            }
        }

        private void GetStatusRequestMessageHandler(GetStatusRequestMessage message)
        {
            Console.WriteLine("GetStatusRequestMessageHandler - Entry point");
            if (!string.IsNullOrWhiteSpace(message.ResponseQueue))
            {
                Console.WriteLine($"GetStatusRequestMessageHandler - sending response to {message.ResponseQueue}");
                _rabbit.PublishContract(message.ResponseQueue, new GetStatusResponseMessage
                {                    
                    StatusText = "All is well",
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

        public void Dispose()
        {
            if (_rabbit != null) { _rabbit.Dispose(); }
        }

        private void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now} (local) - {message}");
            _log.Info(message);
        }
    }
}
