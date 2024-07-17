using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkLogEvents : NetworkPersistentSingleton<NetworkLogEvents>
    {
        public static event Action<LogEvent> OnEventInvoked;

        public static void Add(string id, ulong player, string[] parameters, bool notifySelf = false) 
        {
            Add(new LogEvent(id, true, player, parameters), notifySelf);
        }

        public static void Add(string id, string[] parameters, bool notifySelf = false)
        {
            Add(new LogEvent(id, false, 0, parameters), notifySelf);
        }

        public static void Add(string id, ulong player, bool notifySelf = false)
        {
            Add(new LogEvent(id, true, player, null), notifySelf);
        }

        public static void Add(string id, bool notifySelf = false)
        {
            Add(new LogEvent(id, false, 0, null), notifySelf);
        }

        private static void Add(LogEvent ev, bool notifySelf = false)
        {
            string parameters = ev.parameters != null ? string.Join(LogEvent.separator, ev.parameters) : null;
            Instance.ReplicateEventRpc(ev.id, ev.dateTimeTicks, ev.originatesFromPlayer, ev.player, parameters);

            if(notifySelf)
                OnEventInvoked?.Invoke(ev);
        }

        [Rpc(SendTo.NotMe)]
        protected virtual void ReplicateEventRpc(string id, long dateTimeTicks, bool originatesFromPlayer, ulong player, string parameters)
        {
            OnEventInvoked?.Invoke(new(id, dateTimeTicks, originatesFromPlayer, player, parameters?.Split(';')));
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EditorCheckComponentExistsOnScene()
        {
            if(FindFirstObjectByType<NetworkManager>() != null && FindFirstObjectByType<NetworkLogEvents>() == null)
            {
                Debug.LogError($"A Networking scene should include a {typeof(NetworkLogEvents)} component.");
            }
        }
#endif
    }
}
