using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Lobby
{
    public enum LobbyEnterResponse
    {
        Success = 1,
        DoesntExist = 2,
        NotAllowed = 3,
        Full = 4,
        Error = 5,
        Banned = 6,
        Limited = 7,
        ClanDisabled = 8,
        CommunityBan = 9,
        MemberBlockedYou = 10,
        YouBlockedMember = 11,
        NoRankingDataLobby = 12,
        NoRankingDataUser = 13,
        RankOutOfRange = 14,
        TooManyJoins = 15,
    }
}
