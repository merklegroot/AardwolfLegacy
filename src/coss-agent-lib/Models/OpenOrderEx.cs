using Newtonsoft.Json;
using OpenQA.Selenium;
using trade_model;

namespace trade_browser_lib.Models
{
    public class OpenOrderEx : OpenOrder
    {
        private IWebElement _cancelButton;

        public OpenOrderEx() { }

        public OpenOrderEx(decimal price, decimal quantity, OrderType orderType, IWebElement cancelButton)
        {
            Price = price;
            Quantity = quantity;
            OrderType = orderType;
            _cancelButton = cancelButton;
        }

        public void Cancel()
        {
            _cancelButton.Click();
        }

        public OpenOrder ToBase()
        {
            var contents = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<OpenOrder>(contents);
        }
    }
}
