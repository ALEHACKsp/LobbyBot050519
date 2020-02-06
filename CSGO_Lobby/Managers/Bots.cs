using CSGO_Lobby.Other;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSGO_Lobby
{
    public static class Bots
    {
        public static List<Bot> List;
        private static Logger Logger;
        private static List<Thread> Threads;
        
        // pls no bully me for dis
        private static void HandleThread()
        {
            while (true)
            {
                lock (List)
                {
                    for (var i = 0; i < List.Count; i++)
                    {
                        var bot = List[i];
                        if (bot == null)
                            break;
                        bot.Handle();
                    }
                }

                //Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
        }

        public static bool Start(int threads)
        {
            Logger = new Logger("Bots");
            List = new List<Bot>();
            Threads = new List<Thread>();

            // Load threads
            for (var i = 0; i < threads; i++)
            {
                var thread = new Thread(HandleThread);
                thread.Start();
                Threads.Add(thread);
            }

            //
            Logger.Log($"Started with {threads} threads!");

            // Prepare and login
            foreach (var acc in Accounts.List)
            {
                var bot = new Bot(acc);
                bot.Start();

                Thread.Sleep(10);
                List.Add(bot);
            }

            // Wait until all accounts load
            while (true)
            {
                bool working = false;
                foreach (var bot in List)
                {
                    if (!bot.IsDone)
                        working = true;
                }
                if (!working) break;
            }

            // Push working bots to list
            var validBots = new List<Bot>();
            foreach (var bot in List)
            {
                if (bot.IsSuccess)
                    validBots.Add(bot);
            }
            List.Clear();
            List = validBots;

            Logger.Warn($"Loaded! Total {List.Count} out of {Accounts.List.Count}");
            return List.Count > 0;
        }

        public static void On(EMsg msg, Action<Bot, IPacketMsg> callback)
        {
            foreach (var bot in List)
                bot.On(msg, callback);
        }

        public static List<Bot> GetLobbyValid(int count)
        {
            var bots = new List<Bot>();

            foreach (var bot in List)
            {
                if (bot.IsLobbyValid())
                {
                    bots.Add(bot);
                    if (bots.Count >= count)
                        break;
                }
            }

            return bots;
        }

    }
}
