using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkLogEvents : NetworkPersistenSingletonRequired<NetworkLogEvents>
    {
        public static event Action<LogEvent> OnEventInvoked;

        public static void Add(string id, Color color, string[] parameters, bool notifySelf = false) 
        {
            Instance.Add(new LogEvent(id, color, parameters), notifySelf);
        }

        public static void Add(string id, string[] parameters, bool notifySelf = false)
        {
            Instance.Add(new LogEvent(id, default, parameters), notifySelf);
        }

        public static void Add(string id, Color color, bool notifySelf = false)
        {
            Instance.Add(new LogEvent(id, color, null), notifySelf);
        }

        public static void Add(string id, bool notifySelf = false)
        {
            Instance.Add(new LogEvent(id, default, null), notifySelf);
        }

        private void Add(LogEvent ev, bool notifySelf = false)
        {
            string parameters = ev.parameters != null ? string.Join(LogEvent.separator, ev.parameters) : null;
            ReplicateEventRpc(ev.id, ev.dateTimeTicks, ev.color, parameters);

            if(notifySelf || IsServer)
                OnEventInvoked?.Invoke(ev);
        }

        [Rpc(SendTo.NotMe)]
        protected virtual void ReplicateEventRpc(string id, long dateTimeTicks, Color color, string parameters)
        {
            OnEventInvoked?.Invoke(new(id, dateTimeTicks, color, parameters?.Split(';')));
        }
    }
}
