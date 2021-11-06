using OpenQA.Selenium.Chrome;
using sel_lib;

namespace coss_agent_lib
{
    public class CossWebDriverFactory : ICossWebDriverFactory
    {
        public IRemoteWebDriver Create()
        {
            var driver = new ChromeDriver();
            return new RemoteWebDriverDecorator(driver);
        }
    }
}
