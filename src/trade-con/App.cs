using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace trade_con
{
    public class App : IDisposable
    {
        private class Menu
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public List<MenuItem> MenuItems { get; set; }
        }

        private class MenuItem
        {
            public string Name { get; set; }
            public char Key { get; set; }
            public Action Method { get; set; }
        }

        private Menu _mainMenu;
        private Menu _purchaseMenu;
        private List<Menu> _menus;

        private Guid _currentMenuId;

        private Menu CurrentMenu
        {
            get
            {
                return _menus.FirstOrDefault(item => item.Id == _currentMenuId);
            }
        }

        private bool _keepRunning = true;
        private bool _shouldShowMenu = true;

        public App()
        {
            _mainMenu = new Menu
            {
                Id = Guid.NewGuid(),
                Name = "Main Menu",
                MenuItems = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Name = "(P)urchase",
                        Key = 'P',
                        Method = OnBuySelected
                    },
                    new MenuItem
                    {
                        Name = "e(X)it",
                        Key = 'X',
                        Method = OnExitSelected
                    }
                }
            };

            _purchaseMenu = new Menu
            {
                Id = Guid.NewGuid(),
                Name = "Purchase Menu",
                MenuItems = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Name = "(B)ack",
                        Key = 'B',
                        Method = PurchaseMenu_Back
                    },
                    new MenuItem
                    {
                        Name = "(1) LSK / ETH",
                        Key = '1',
                        Method = () => { }
                    }
                }
            };

            _menus = new List<Menu> { _mainMenu, _purchaseMenu };
            _currentMenuId = _mainMenu.Id;            
        }

        public void Run()
        {   
            while (_keepRunning)
            {
                if (_shouldShowMenu) { ShowMenu(); }
                ProcessKey(Console.ReadKey(true).KeyChar);
            }
        }

        private void ProcessKey(char key)
        {
            var match = CurrentMenu.MenuItems.FirstOrDefault(item => string.Equals(item.Key.ToString(), key.ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (match != null)
            {
                match.Method();
                _shouldShowMenu = true;
            }
        }

        public void Dispose()
        {
        }

        private void ShowMenu()
        {
            var menuBuilder = new StringBuilder();
            menuBuilder.AppendLine($"[-- {CurrentMenu.Name} --]");
            menuBuilder.AppendLine();
            foreach(var item in CurrentMenu.MenuItems)
            {
                menuBuilder.AppendLine($"  {item.Name}");
            }

            Console.WriteLine(menuBuilder.ToString());

            _shouldShowMenu = false;
        }

        private void OnExitSelected()
        {
            _keepRunning = false;
        }

        private void OnBuySelected()
        {
            _currentMenuId = _purchaseMenu.Id;
        }

        private void PurchaseMenu_Back()
        {
            _currentMenuId = _mainMenu.Id;
        }
    }
}
