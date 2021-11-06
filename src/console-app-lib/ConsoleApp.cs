using System;
using System.Collections.Generic;
using System.Linq;

namespace console_app_lib
{
    public abstract class ConsoleApp
    {
        protected virtual bool KeepRunning { get; set; }

        public void Run()
        {
            KeepRunning = true;

            ShowMenu();
            while (KeepRunning)
            {
                var key = Console.ReadKey(true);
                ProcessKey(key.KeyChar);
            }
        }

        protected abstract List<MenuItem> Menu { get; }

        private List<MenuItem> MenuWithExit
        {
            get
            {
                return (Menu ?? new List<MenuItem>()).Union(new List<MenuItem>
                {
                    new MenuItem("e(X)it", 'X', () => KeepRunning = false)
                }).ToList();
            }
        }

        private void ShowMenu()
        {
            foreach (var menuItem in MenuWithExit)
            {
                Console.WriteLine(menuItem.DisplayText);
            }

            Console.WriteLine();
        }

        private void ProcessKey(char key)
        {
            var menuItem = MenuWithExit.SingleOrDefault(queryMenuItem =>
                char.ToUpperInvariant(queryMenuItem.Key) == char.ToUpperInvariant(key));

            if (menuItem != null)
            {
                try
                {
                    menuItem.Method();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                Console.WriteLine("");
                ShowMenu();
            }
        }
    }
}
