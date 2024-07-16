using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public struct NetworkEvent
    {
        public const char separator = ';';

        public readonly bool IsPlayerEvent => player > 0;

        public string id;
        public ulong player;
        public string[] parameters;

        public NetworkEvent(string id, ulong player, string[] parameters)
        {
            this.id = id;
            this.player = player;
            this.parameters = parameters;
        }
    }

    public class NetworkEvents : NetworkPersistentSingleton<NetworkEvents>
    {
        public static event Action<NetworkEvent> OnEventInvoked;

        public static void Add(string id, ulong player, string[] parameters, bool notifySelf = false) 
        {
            Add(new NetworkEvent(id, player, parameters), notifySelf);
        }

        public static void Add(string id, string[] parameters, bool notifySelf = false)
        {
            Add(new NetworkEvent(id, 0, parameters), notifySelf);
        }

        public static void Add(string id, ulong player, bool notifySelf = false)
        {
            Add(new NetworkEvent(id, player, null), notifySelf);
        }

        public static void Add(string id, bool notifySelf = false)
        {
            Add(new NetworkEvent(id, 0, null), notifySelf);
        }

        public static void Add(NetworkEvent ev, bool notifySelf = false)
        {
            string parameters = string.Join(NetworkEvent.separator, ev.parameters);
            Instance.ReplicateEventRpc(ev.id, ev.player, parameters);

            if(notifySelf)
                OnEventInvoked?.Invoke(ev);
        }

        [Rpc(SendTo.NotMe)]
        protected virtual void ReplicateEventRpc(string id, ulong player, string parameters)
        {
            OnEventInvoked?.Invoke(new(id, player, parameters.Split(';')));
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EditorCheckComponentExistsOnScene()
        {
            if(FindFirstObjectByType<NetworkManager>() != null && FindFirstObjectByType<NetworkEvents>() == null)
            {
                Debug.LogError($"A Networking scene should include a {typeof(NetworkEvents)} component.");
            }
        }
#endif
    }
}
