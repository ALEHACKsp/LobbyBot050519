using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Lobby.Other
{
    public class Logger
    {
        private string Prefix;

        public Logger(string prefix)
        {
            Prefix = prefix;
        }

        private void Log(string prefix, ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            foreach (var line in text.Split('\n'))
                Console.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()} [{Prefix}] [{prefix}] {line}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Log(string text)
        {
            Log("LOG", ConsoleColor.White, text);
        }

        public void Warn(string text)
        {
            Log("WARNING", ConsoleColor.Yellow, text);
        }

        public void Error(string text)
        {
            Log("ERROR", ConsoleColor.Red, text);
        }
    }
}
