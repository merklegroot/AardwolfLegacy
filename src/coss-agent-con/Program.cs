﻿namespace coss_agent_con
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var app = new App())
            {
                app.Run();
            }
        }
    }
}
