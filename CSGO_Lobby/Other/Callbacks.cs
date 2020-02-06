using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Lobby.Other
{
    //internal class Callback
    //{
    //    private Type Type;
    //    private Action<object> Action;

    //    public Callback(Type type, Action<object> action)
    //    {
    //        Type = type;
    //        Action = action;
    //    }

    //    public void Fire()
    //    {

    //    }
    //}

    //public class Callbacks
    //{
    //    private Dictionary<EMsg, List<Callback>> List;

    //    public Callbacks()
    //    {
    //        List = new Dictionary<EMsg, List<Callback>>();
    //    }

    //    public void On<TCallback>(EMsg msg, Action<TCallback> callback)
    //        where TCallback : class, IPacketMsg
    //    {
    //        List[msg].Add(new Callback(TCallback, callback));
    //    }

    //    public void Fire(IPacketMsg msg)
    //    {
    //        if (List.ContainsKey(msg.MsgType))
    //        {
    //            foreach (var callback in List[msg.MsgType])
    //                callback.Fire();
    //        }
    //    }
    //}
}
