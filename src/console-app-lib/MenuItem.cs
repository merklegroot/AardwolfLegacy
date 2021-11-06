using System;

namespace console_app_lib
{
    public class MenuItem
    {
        public MenuItem() { }
        public MenuItem(string displayText, char key, Action method)
        {
            DisplayText = displayText;
            Key = key;
            Method = method;
        }

        public string DisplayText { get; set; }
        public char Key { get; set; }
        public Action Method { get; set; }
    }
}
