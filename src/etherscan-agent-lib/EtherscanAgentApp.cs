using assembly_lib;
using browser_lib;
using config_client_lib;
using config_lib;
using etherscan_lib;
using etherscan_lib.Models;
using log_lib;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using rabbit_lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using trade_constants;
using trade_contracts;
using wait_for_it_lib;

namespace etherscan_agent_lib
{
    public class EtherscanAgentApp : IEtherscanAgentApp
    {
        public const string ApplicationName = "Etherscan Agent";
        private IRabbitConnection _rabbit;

        private readonly IBrowserUtil _browserUtil;
        private readonly IConfigClient _configClient;
        private readonly IEtherscanHoldingRepo _holdingRepo;
        private readonly IEtherscanHistoryRepo _historyRepo;
        private readonly IWaitForIt _waitForIt;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly ILogRepo _log;

        private RemoteWebDriver Driver { get { return _browserUtil.Driver; } }

        public EtherscanAgentApp(
            IConfigClient configClient,
            IBrowserUtil browserUtil,
            IEtherscanHoldingRepo etherscanRepo,
            IEtherscanHistoryRepo historyRepo,
            IWaitForIt waitForIt,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
        {
            _browserUtil = browserUtil;
            _configClient = configClient;
            _holdingRepo = etherscanRepo;
            _historyRepo = historyRepo;
            _waitForIt = waitForIt;
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _log = log;
        }

        private bool _keepRunning = true;
                
        public void Run()
        {
            _rabbit = _rabbitConnectionFactory.Connect();
            _rabbit.Listen(TradeRabbitConstants.Queues.EtherscanAgentQueue, OnMessageReceived);

            Info($"{ApplicationName} running and connected.");
            while (_keepRunning)
            {
                Sleep(TimeSpan.FromMilliseconds(500));
            }
        }

        public void RunTest()
        {
            const string CossAddress = "0x0d6b5a54f940bf3d52e438cab785981aaefdf40c";
            NavigateToTokenTransactionsForAddress(CossAddress);
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
                else if (string.Equals(messageTypeText, typeof(UpdateFundsRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<UpdateFundsRequestMessage>(messagePayload);
                    Handle(contract);
                    return;
                }
                else if (string.Equals(messageTypeText, typeof(UpdateHistoryRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<UpdateHistoryRequestMessage>(messagePayload);
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

        private static object _slimLocker = new object();
        private static ManualResetEventSlim _slim = new ManualResetEventSlim(true);
        private void Handle(UpdateHistoryRequestMessage message)
        {
            lock (_slimLocker)
            {
                if (!_slim.IsSet)
                {
                    Info("Already updating transactions.");
                    return;
                }

                _slim.Wait();
                _slim.Reset();
            }

            try
            {
                // Info("Updating History...");
                // UpdateHistory();

                Info("Updating Transactions...");

                var hashes = _historyRepo.GetTransactionHashes();
                if (hashes == null || !hashes.Any())
                {
                    Info("There are no transactions to get history from.");
                    return;
                }

                for (var i = 0; i < hashes.Count; i++)
                {
                    Console.WriteLine($"Hash {i+1} of {hashes.Count}.");
                    var hash = hashes[i];
                    
                    UpdateTransaction(hash);
                }
            }
            catch(Exception exception)
            {
                _log.Error(exception);
            }
            finally
            {
                _slim.Set();
            }
        }

        private DateTime? _lastUpdateTime;

        private static object UpdateTransactionLocker = new object();
        private bool UpdateTransaction(string hash)
        {
            lock (UpdateTransactionLocker)
            {
                return UpdateTransactionUnwrapped(hash);
            }
        }

        private static TimeSpan TimeBetweenBrowserUpdates = TimeSpan.FromMinutes(2.5);
        // private static TimeSpan TimeBetweenBrowserUpdates = TimeSpan.FromMinutes(2.5);
        // private static TimeSpan TimeBetweenBrowserUpdates = TimeSpan.FromSeconds(5); 

        private bool UpdateTransactionUnwrapped(string hash)
        {
            var retrievedTransaction = _historyRepo.GetTransaction(hash);
            if (retrievedTransaction != null)
            {
                Console.WriteLine("Already up to date.");
                return true;
            }

            Console.WriteLine($"Updating Transaction {hash}");

            if (_lastUpdateTime.HasValue)
            {
                var timeSince = DateTime.UtcNow - _lastUpdateTime.Value;
                var timeRemaining = TimeBetweenBrowserUpdates - timeSince;
                if (timeRemaining > TimeSpan.Zero)
                {
                    Sleep(timeRemaining);
                }                
            }

            _lastUpdateTime = DateTime.UtcNow;

            NavigateToTransaction(hash);

            var mainDiv = Driver.FindElementById("ContentPlaceHolder1_maintable");
            if (mainDiv == null) { return false; }
            var cells = mainDiv.FindElements(By.TagName("div"));
            string key = null;
            string value = null;

            var data = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (i % 2 == 0)
                {
                    key = cell.Text;
                }
                else
                {
                    value = cell.Text;
                    data.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            var container = new EtherscanTransactionContainer
            {
                TimeStampUtc = DateTime.Now,
                TransactionHash = hash,
                Data = data
            };

            _historyRepo.InsertTransaction(container);

            Console.WriteLine($"Done updating transaction {hash}");

            return true;
        }

        private bool UpdateHistory()
        {
            Console.WriteLine("Updating History...");

            var etherScanRows = new List<List<string>>();
            
            int page = 1;
            var keepGoing = true;

            NavigateToTransactionsList();// page != 1 ? page : (int?)null);
            do
            {
                var table = Driver.FindElementsByTagName("table")
                    .FirstOrDefault(queryTable =>
                    {
                        queryTable.FindElements(By.TagName("th"))
                        .Any(header => string.Equals((header.Text ?? string.Empty).Trim(), "TxHash", StringComparison.InvariantCultureIgnoreCase));
                        return true;
                    });

                if (table == null) { break; }

                var tableBody = table.FindElement(By.TagName("tbody"));
                if (tableBody == null) { break; }

                var rows = tableBody.FindElements(By.TagName("tr"));
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var cells = row.FindElements(By.TagName("td"));
                        var cellTexts = cells.Select(item => item.Text).ToList();
                        etherScanRows.Add(cellTexts);
                    }
                }

                var nextButton = Driver.FindElementsByTagName("a")
                    .FirstOrDefault(item => string.Equals(item.Text, "Next", StringComparison.InvariantCultureIgnoreCase));

                if (nextButton == null
                    || !nextButton.Enabled
                    || string.Equals(nextButton.GetAttribute("disabled"), "true", StringComparison.InvariantCultureIgnoreCase))
                {
                    keepGoing = false;
                }
                else
                {
                    nextButton.SendKeys(Keys.Enter);
                }

                page++;
            } while (keepGoing && page < 1000);

            var container = new EtherscanTransactionHistoryContainer
            {
                TimeStampUtc = DateTime.UtcNow,
                Rows = etherScanRows
            };

            _historyRepo.Insert(container);

            return true;
        }

        private void NavigateToTokenTransactionsForAddress(string address, int page = 0)
        {
            if (string.IsNullOrWhiteSpace(address)) { throw new ArgumentNullException(nameof(address)); }
            if (page < 0) { throw new ArgumentOutOfRangeException(nameof(page)); }
        }

        private void NavigateToTransaction(string transactionHash)
        {
            if (string.IsNullOrWhiteSpace(transactionHash)) { throw new ArgumentNullException(nameof(transactionHash)); }
            var url = $"https://etherscan.io/tx/{transactionHash}";
            Driver.Navigate().GoToUrl(url);
        }

        private void NavigateToTransactionsList(int? page = null)
        {
            var walletAddress = _configClient.GetMewWalletAddress();
            var url = $"https://etherscan.io/txs?a={walletAddress}";
            if (page.HasValue) { url += $"&p={page}"; }
            Driver.Navigate().GoToUrl(url);
        }

        private void NavigateToWallet()
        {
            var walletAddress = _configClient.GetMewWalletAddress();
            var url = $"https://etherscan.io/address/{walletAddress}";
            Driver.Navigate().GoToUrl(url);
        }

        private static object UpdateFundsLocker = new object();
        private static bool _isUpdatingFunds = false;
        private void Handle(UpdateFundsRequestMessage message)
        {
            UpdateFunds();
        }

        public void UpdateFunds()
        {
            if (_isUpdatingFunds)
            {
                Info("Already updating funds.");
                return;
            }

            lock (UpdateFundsLocker)
            {
                if (_isUpdatingFunds)
                {
                    Info("Already updating funds.");
                    return;
                }

                try
                {
                    UpdateFundsUnwrapped();
                }
                finally
                {
                    _isUpdatingFunds = false;
                }
            }
        }

        private bool UpdateFundsUnwrapped(int attemptIndex = 0)
        {
            Info("Updating Holdings...");
            try
            {
                NavigateToTokenHoldings();

                var tableBody = _browserUtil.WaitForElement(() =>
                {
                    return Driver.FindElementById("ContentPlaceHolder1_divresult")?.FindElement(By.TagName("tbody"));
                });

                if (tableBody == null) { return false; }

                var rows = tableBody.FindElements(By.TagName("tr"));
                if (rows == null) { return true; }

                var etherScanRows = new List<List<string>>();
                foreach (var row in rows)
                {
                    var cells = row.FindElements(By.TagName("td"));
                    if (cells == null || !cells.Any()) { continue; }

                    var etherScanRow = cells.Select(item => item.Text).ToList();

                    etherScanRows.Add(etherScanRow);
                }

                var container = new EtherScanTokenHoldingContainer
                {
                    TimeStampUtc = DateTime.UtcNow,
                    Rows = etherScanRows
                };

                _holdingRepo.Insert(container);

                Info("Successfully updated holdings.");

                return true;
            }
            catch (InvalidOperationException exception)
            {
                _log.Error(exception);
                try
                {
                    _browserUtil.RestartDriver();

                    if (attemptIndex == 0)
                    {
                        return UpdateFundsUnwrapped(attemptIndex + 1);
                    }
                }
                catch(Exception exceptionB)
                {
                    _log.Error(exceptionB);
                    return false;
                }

                return false;
            }
            catch(Exception exception)
            {
                _log.Error(exception);
                return false;
            }
        }

        private bool NavigateToTokenHoldings()
        {
            var walletAddress = _configClient.GetMewWalletAddress();
            var url = $"https://etherscan.io/tokenholdings?a={walletAddress}";

            _browserUtil.GoToUrl(url);

            return _waitForIt.Wait(() => Driver.Title.ToUpper().Contains("Ethereum Token Holdings".ToUpper()));

            //if (!result)
            //{
            //    _browserUtil.RestartDriver();
            //}

            //Driver.Navigate().GoToUrl(url);

            //return _waitForIt.Wait(() => Driver.Title.ToUpper().Contains("Ethereum Token Holdings".ToUpper()));
        }

        public void Dispose()
        {
            if(_browserUtil != null) { _browserUtil.Dispose(); }
        }

        private void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now} (local) - {message}");
            _log.Info(message);
        }

        private void Sleep(TimeSpan timeSpan)
        {
            var maxSleepTimeSpan = TimeSpan.FromMilliseconds(100);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            TimeSpan timeRemaining;
            while ((timeRemaining = timeSpan - stopWatch.Elapsed) > TimeSpan.Zero)
            {
                Thread.Sleep(timeRemaining >= maxSleepTimeSpan ? maxSleepTimeSpan : timeRemaining);
            }
        }
    }
}
