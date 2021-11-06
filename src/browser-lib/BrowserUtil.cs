using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using wait_for_it_lib;

namespace browser_lib
{
    public class BrowserUtil : IBrowserUtil
    {
        private const string DriverKey = "driver";
        private readonly IWaitForIt _waitForIt;

        public BrowserUtil(IWaitForIt waitForIt)
        {
            _waitForIt = waitForIt;
        }

        private Dictionary<string, object> _propertyContainer = new Dictionary<string, object>();

        private object DriverLocker = new object();
        public RemoteWebDriver Driver
        {
            get
            {                
                if (_propertyContainer.ContainsKey(DriverKey)) { return (RemoteWebDriver)_propertyContainer[DriverKey]; }

                lock (DriverLocker)
                {
                    if (_propertyContainer.ContainsKey(DriverKey)) { return (RemoteWebDriver)_propertyContainer[DriverKey]; }                   
                    return (RemoteWebDriver)(_propertyContainer[DriverKey] = new ChromeDriver());
                }
            }
        }
        
        public bool SetInputTextAndVerify(IWebElement input, string text)
        {
            input.Clear();
            input.SendKeys(text);

            if (!string.Equals(text, GetInputText(input), StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public string GetInputText(IWebElement input)
        {
            return input.GetAttribute("value");
        }

        public void Dispose()
        {
            Driver.Dispose();
        }

        public bool PerformSteps(List<Func<bool>> steps)
        {
            foreach (var step in steps)
            {
                if (!step()) { return false; }
            }

            return true;
        }

        public IWebElement WaitForElement(Func<IWebElement> method)
        {
            IWebElement element = null;

            _waitForIt.Wait(() =>
            {
                try
                {
                    element = method();
                    return element != null;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(30));

            return element;
        }


        public ReadOnlyCollection<IWebElement> WaitForElements(Func<ReadOnlyCollection<IWebElement>> method)
        {
            ReadOnlyCollection<IWebElement> element = null;

            _waitForIt.Wait(() =>
            {
                try
                {
                    element = method();
                    return element != null && element.Count > 0;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(30));

            return element;
        }

        public void RestartDriver()
        {
            Driver.Dispose();
            _propertyContainer[DriverKey] = new ChromeDriver();
        }

        public void GoToUrl(string url)
        {
            try
            {
                Driver.Navigate().GoToUrl(url);
            }
            catch
            {
                RestartDriver();
                Driver.Navigate().GoToUrl(url);
            }
        }

        public void Sleep(TimeSpan timeSpan)
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
