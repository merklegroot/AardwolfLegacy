using assembly_lib;
using env_config_lib;
using log_lib;
using mew_agent_con.Models;
using Newtonsoft.Json;
using rabbit_lib;
using StructureMap;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using trade_constants;
using trade_contracts;
using trade_res;

namespace mew_agent_con
{
    public class MewApp : IMewApp
    {
        public const string ApplicationName = "Mew Agent";
        private IRabbitConnection _rabbit;
        private static ILogRepo _log;

        public MewApp(ILogRepo log)
        {
            _log = log;
        }

        private class Exchange
        {
            public const string Binance = "binance";
            public const string HitBtc = "hitbtc";
            public const string Qryptos = "qryptos";
            public const string Coss = "coss";
            public const string KuCoin = "kucoin";
            public const string Livecoin = "livecoin";
        }

        public void Run()
        {
            var container = Container.For<MewAgentRegistry>();
            using (var browser = container.GetInstance<IMewBrowser>())
            {
                // browser.Send(CommodityRes.Poe, QuantityToSend.All, Exchange.Coss);
                browser.Send(CommodityRes.Eth, QuantityToSend.Some(4.4m), Exchange.Coss);
            }
        }

        public void RunOld()
        {
            Console.WriteLine(ApplicationName);
            Console.WriteLine("Connecting to rabbit...");
            
            try
            {
                _rabbit = new RabbitConnectionFactory(new EnvironmentConfigRepo()).Connect();
                _rabbit.Listen(TradeRabbitConstants.Queues.MewAgentQueue, response =>
                {
                    try
                    {
                        OnMessageReceived(response);
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }
                });
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to connect to rabbit.");
                Console.WriteLine(exception.Message);
                return;
            }

            Console.WriteLine("Connected to rabbit successfully.");

            Console.WriteLine("e(X)it");
            char ch;
            do
            {
                ch = Console.ReadKey(true).KeyChar;
            } while (char.ToUpper(ch) != 'X');

            _rabbit.Dispose();
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

                if (string.Equals(messageTypeText, typeof(MewLoginRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<MewLoginRequestMessage>(messagePayload);
                    Handle(contract);
                    return;
                }

                if (string.Equals(messageTypeText, typeof(WithdrawFundsRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<WithdrawFundsRequestMessage>(messagePayload);
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

        private void Handle(MewLoginRequestMessage message)
        {
            Console.WriteLine("Performing login");
            
            var container = Container.For<MewAgentRegistry>();
            using (var browser = container.GetInstance<IMewBrowser>())
            {
                browser.Login();
            }

            Console.WriteLine("Login operation complete.");
        }

        private void Handle(WithdrawFundsRequestMessage message)
        {
            throw new NotImplementedException();

            //Console.WriteLine("Performing withdraw funds");
            //var container = Container.For<MewAgentRegistry>();

            //using (var browser = container.GetInstance<IMewBrowser>())
            //{
            //    browser.TransferFunds(message.Commodity, message.DepositAddress);
            //}

            //Console.WriteLine($"Not implemented for {message.Commodity}");

            //Console.WriteLine("Withdraw funds operation complete.");
        }
    }
}
