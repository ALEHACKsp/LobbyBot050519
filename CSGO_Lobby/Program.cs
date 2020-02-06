using CSGO_Lobby.Other;
using System.Linq;
using System.Threading;

namespace CSGO_Lobby
{
    public class Program
    {
        private static Logger Logger;
        
        public static void Main()
        {
            Logger = new Logger("Program");
            Logger.Log("Initializing...");

            Logger.Log("Loading accounts...");
            Accounts.Load();

            Logger.Log("Initializing bots...");
            Bots.Start(8);

            Logger.Log("Running...");
            LobbyBot.Run();
        }
    }
}
