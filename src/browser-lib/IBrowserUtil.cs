using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.ObjectModel;

namespace browser_lib
{
    public interface IBrowserUtil : IDisposable
    {
        RemoteWebDriver Driver { get; }

        IWebElement WaitForElement(Func<IWebElement> method);
        ReadOnlyCollection<IWebElement> WaitForElements(Func<ReadOnlyCollection<IWebElement>> method);

        void RestartDriver();

        void GoToUrl(string url);

        bool SetInputTextAndVerify(IWebElement input, string text);

        string GetInputText(IWebElement input);

        void Sleep(TimeSpan timeSpan);
    }
}
