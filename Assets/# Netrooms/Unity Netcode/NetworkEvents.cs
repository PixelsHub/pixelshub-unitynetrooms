using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public struct NetworkEvent : INetworkSerializable
    {
        public string id;
        public bool isPlayerEvent;
        public ulong player;
        public string[] parameters;

        public NetworkEvent(string id, bool isPlayerEvent, ulong player, string[] parameters)
        {
            this.id = id;
            this.isPlayerEvent = isPlayerEvent;
            this.player = player;
            this.parameters = parameters;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref isPlayerEvent);
            serializer.SerializeValue(ref player);

            if(parameters != null)
                for(int i = 0; i < parameters.Length; i++)
                    serializer.SerializeValue(ref parameters[i]);
        }
    }

    public class NetworkEvents : NetworkPersistentSingleton<NetworkEvents>
    {
        public static event Action<NetworkEvent> OnEventInvoked;

        public void Add(string id, ulong player, string[] parameters, bool notifySelf = false) 
        {
            Add(new NetworkEvent(id, true, player, parameters), notifySelf);
        }

        public void Add(string id, string[] parameters, bool notifySelf = false)
        {
            Add(new NetworkEvent(id, false, 0, parameters), notifySelf);
        }

        public void Add(string id, ulong player, bool notifySelf = false)
        {
            Add(new NetworkEvent(id, true, player, null), notifySelf);
        }

        public void Add(string id, bool notifySelf = false)
        {
            Add(new NetworkEvent(id, false, 0, null), notifySelf);
        }

        public void Add(NetworkEvent networkEvent, bool notifySelf = false)
        {
            ReplicateEventRpc(networkEvent);

            if(notifySelf)
                OnEventInvoked?.Invoke(networkEvent);
        }

        [Rpc(SendTo.NotMe)]
        protected virtual void ReplicateEventRpc(NetworkEvent networkEvent)
        {
            OnEventInvoked?.Invoke(networkEvent);
        }

#if UNITY_EDITOR
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
