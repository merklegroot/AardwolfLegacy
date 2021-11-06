using env_config_lib;
using env_config_lib.Model;
using Newtonsoft.Json;
using parse_lib;
using System;
using System.Collections.Generic;

namespace env_config_con
{
    public class EnvConfigApp
    {
        private const string ApplicationName = "EnvConfig Console";

        private readonly IEnvironmentConfigRepo _envConfigRepo;

        private class MenuItem
        {
            public MenuItem() { }
            public MenuItem(char key, string text, Action method)
            {
                Key = key;
                Text = text;
                Method = method;
            }

            public char Key { get; set; }
            public string Text { get; set; }
            public Action Method { get; set; }
        }

        private List<MenuItem> Menu => new List<MenuItem>
        {
            new MenuItem('G', "(G)et", new Action(() => OnGetSelected())),
            new MenuItem('H', "set (H)ost", new Action(() => OnSetHostSelected())),
            new MenuItem('U', "set (U)ser Name", new Action(() => OnSetUserNameSelected())),
            new MenuItem('P', "set (P)assword", new Action(() => OnSetPasswordSelected())),
            new MenuItem('N', "set port (N)umber", new Action(() => OnSetPortNumberSelected())),
            new MenuItem('E', "(E)nable SSL", new Action(() => OnEnableSslSelected())),
            new MenuItem('D', "(D)isable SSL", new Action(() => OnDisableSslSelected())),
            new MenuItem('X', "e(X)it", new Action(() => OnExitAppSelected()))
        };

        private bool _keepRunning;

        public EnvConfigApp(IEnvironmentConfigRepo envConfigRepo)
        {
            _envConfigRepo = envConfigRepo;
        }

        public void Run()
        {
            Console.WriteLine($"{ApplicationName}");

            ShowMenu();
            _keepRunning = true;
            while (_keepRunning)
            {
                var key = Console.ReadKey(true);
                ProcessKey(key.KeyChar);
            }
        }

        private void ProcessKey(char key)
        {
            foreach (var menuItem in Menu)
            {
                if (char.ToUpper(menuItem.Key) == char.ToUpper(key))
                {
                    menuItem.Method();
                    return;
                }
            }
        }

        private void ShowMenu()
        {
            Console.WriteLine();
            foreach (var menuItem in Menu)
            {
                Console.WriteLine(menuItem.Text);
            }

            Console.WriteLine();
        }

        private void OnExitAppSelected()
        {
            _keepRunning = false;
        }

        private void OnGetSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig();
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }

        private void OnSetHostSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig() ?? new RabbitClientConfig();
            Console.WriteLine($"Current Host: {config?.Host ?? string.Empty}");
            Console.Write("Enter Host Name > ");
            var hostContents = Console.ReadLine();
            config.Host = hostContents != null ? hostContents.Trim() : null;

            _envConfigRepo.SetRabbitClientConfig(config);
            Console.WriteLine("Host has been set.");
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }
        
        private void OnSetUserNameSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig() ?? new RabbitClientConfig();
            Console.WriteLine($"Current User Name: {config?.UserName ?? string.Empty}");
            Console.WriteLine("Enter User Name > ");
            var textContents = Console.ReadLine();
            config.UserName = textContents != null ? textContents.Trim() : null;

            _envConfigRepo.SetRabbitClientConfig(config);
            Console.WriteLine("User Name has been set.");
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }

        private void OnSetPasswordSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig() ?? new RabbitClientConfig();
            Console.WriteLine($"Current Password: {config?.Password ?? string.Empty}");
            Console.WriteLine("Enter Password > ");
            var textContents = Console.ReadLine();
            config.Password = textContents != null ? textContents.Trim() : null;

            _envConfigRepo.SetRabbitClientConfig(config);
            Console.WriteLine("Password has been set.");
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }

        private void OnSetPortNumberSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig() ?? new RabbitClientConfig();
            var portText = config != null ? config.Port.ToString() : string.Empty;
            Console.WriteLine($"Current Port Number: {portText}");
            Console.WriteLine("Enter Port Number > ");
            var textContents = Console.ReadLine();
            var num = ParseUtil.IntTryParse(textContents);
            if (!num.HasValue)
            {
                Console.WriteLine("That's not a number.");
                ShowMenu();
                return;
            }

            config.Port = num.Value;

            _envConfigRepo.SetRabbitClientConfig(config);
            Console.WriteLine("Port has been set.");
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }

        private void OnEnableSslSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig() ?? new RabbitClientConfig();
            config.UseSsl = true;

            _envConfigRepo.SetRabbitClientConfig(config);

            Console.WriteLine("SSL has been turned on.");
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }

        private void OnDisableSslSelected()
        {
            var config = _envConfigRepo.GetRabbitClientConfig() ?? new RabbitClientConfig();
            config.UseSsl = false;

            _envConfigRepo.SetRabbitClientConfig(config);

            Console.WriteLine("SSL has been turned off.");
            var contents = JsonConvert.SerializeObject(config);
            Console.WriteLine(contents);

            ShowMenu();
        }
    }
}
