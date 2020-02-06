using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Lobby
{
    public class Account
    {
        public string Username;
        public string Password;
    }

    public static class Accounts
    {
        public static List<Account> List = new List<Account>();

        private static readonly string Path = "accounts.txt";

        public static bool Load()
        {
            if (!File.Exists(Path))
                return false;

            var content = File.ReadAllLines(Path);
            foreach (var line in content)
            {
                var v = line.Split(':');
                if (v.Length != 2)
                    continue;
                if (v[0].StartsWith("//"))
                    continue;

                List.Add(new Account()
                {
                    Username = v[0],
                    Password = v[1],
                });
            }

            return List.Count > 0;
        }
    }
}
