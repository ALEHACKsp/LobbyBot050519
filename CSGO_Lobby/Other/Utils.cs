using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using ValveKeyValue;

namespace CSGO_Lobby.Other
{
    public static class Utils
    {
        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static uint RandomPID()
        {
            var pid = (uint)(new Random().Next(1000, 20000));
            while ((pid % 4) != 0) pid++;
            return pid;
        }
    }
}
