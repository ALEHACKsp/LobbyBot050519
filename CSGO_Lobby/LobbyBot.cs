using CSGO_Lobby.Other;
using SteamKit2;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSGO_Lobby
{
    public static class LobbyBot
    {
        private static Logger Logger;

        public static void Run()
        {
            Logger = new Logger("LobbyBot");
            var monitorBot = Bots.List.First();
            ulong lastLobbyId = 0;
            var handledLobbies = new List<ulong>();

            monitorBot.On(EMsg.ClientMMSCreateLobbyResponse, (_, msg) =>
            {
                var response = new ClientMsgProtobuf<CMsgClientMMSCreateLobbyResponse>(msg);

                var delta = (response.Body.steam_id_lobby - 1) - (lastLobbyId + 1);
                Logger.Log($"Lobby created! Delta: {delta}");

                if (lastLobbyId != 0)
                {
                    for (ulong i = 0; i < delta; i++)
                    {
                        monitorBot.GetLobbyData(response.Body.steam_id_lobby - 1 - i);
                    }
                }

                lastLobbyId = response.Body.steam_id_lobby;
                monitorBot.CreateLobby();
            });

            Bots.On(EMsg.ClientMMSJoinLobbyResponse, (bot, msg) =>
            {
                var response = new ClientMsgProtobuf<CMsgClientMMSJoinLobbyResponse>(msg);
                var code = (LobbyEnterResponse)response.Body.chat_room_enter_response;

                if (code == LobbyEnterResponse.Success)
                    Logger.Log($"Joined: {code}");
                else
                {
                    if (code == LobbyEnterResponse.TooManyJoins)
                    {
                        Logger.Error($"Joined: {code}");

                        //var joinBots = Bots.GetLobbyValid(1);
                        //if (joinBots.Count > 0)
                        //    joinBots[0].JoinLobby(response.Body.steam_id_lobby);
                    }
                    else
                        Logger.Warn($"Joined: {code}");
                }

                if (response.Body.chat_room_enter_response == 1)
                {
                    bot.LobbySendJoinData(response.Body.steam_id_lobby, response.Body.steam_id_owner);
                    bot.LobbySendChatMsg(response.Body.steam_id_lobby, "GET GOOD GET VK.COM/INTERWEBZMEMES");
                    //bot.LobbySendChatMsg(response.Body.steam_id_lobby, "VAVLE PLS FIX (PROPERLY)\ngithub.com/click4dylan/CSGO_FakeAngleFix");
                    bot.LobbySendChatMsg(response.Body.steam_id_lobby, "ALSO JOIN OUR DISCORD\nhttps://discord.gg/AmqfxWE");
                    Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(x =>
                    {
                        bot.LobbyCrash(response.Body.steam_id_lobby);
                        bot.LeaveLobby(response.Body.steam_id_lobby);
                    });
                }
            });

            Bots.On(EMsg.ClientMMSLobbyData, (bot, msg) =>
            {
                var response = new ClientMsgProtobuf<CMsgClientMMSLobbyData>(msg);

                if (response.Body.app_id == 730 &&
                    response.Body.num_members != 0 &&
                    response.Body.num_members != response.Body.max_members/* &&
                    response.Body.lobby_flags == 0*/)
                {
                    // We are getting lobbydata even when we are chillin in lobby
                    if (handledLobbies.Contains(response.Body.steam_id_lobby))
                        return;
                    handledLobbies.Add(response.Body.steam_id_lobby);

                    var joinBots = Bots.GetLobbyValid(1);
                    if (joinBots.Count < 1)
                    {
                        Logger.Error("Out of valid bots!!!");
                        return;
                    }
                    
                    joinBots[0].JoinLobby(response.Body.steam_id_lobby);
                }
            });

#if DEBUG
            monitorBot.JoinLobby(109775241011612917);
#else
            //monitorBot.CreateLobby();
#endif

            Thread.Sleep(-1);
        }
    }
}
