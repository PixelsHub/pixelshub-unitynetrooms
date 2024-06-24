using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json;

namespace PixelsHub.Netrooms
{
    public class NetworkChat : HttpInitializedNetworkBehaviour
    {
        public class Message : INetworkSerializable
        {
            public long ticks;
            public string author;
            public string text;

            public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ticks);
                serializer.SerializeValue(ref author);
                serializer.SerializeValue(ref text);
            }
        }

        public event Action<Message> OnMessageAdded;

        public override string HttpUrlPath => "chat";

        private List<Message> messages;

        public void AddMessage(Message message) 
        {
            message.ticks = DateTime.UtcNow.Ticks;
            messages.Add(message);
            OnMessageAdded?.Invoke(message);

            AddMessageNotMeRpc(message);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(IsServer)
                messages = new();
        }

        [Rpc(SendTo.NotMe)]
        private void AddMessageNotMeRpc(Message message)
        {
            InsertMessage(message);
        }

        protected override async void ProcessHttpInitialization(HttpContent content)
        {
            Debug.Assert(!IsServer);

            string json = await content.ReadAsStringAsync();

            var replicatedMessages = JsonConvert.DeserializeObject<List<Message>>(json);

            Debug.Assert(messages == null);
            Debug.Assert(replicatedMessages != null);

            messages = replicatedMessages;
        }

        private async void InsertMessage(Message message)
        {
            while(!isHttpInitializationCompleted)
                await Task.Delay(200);

            Debug.Log($"[Chat] {message.text}");

            if(messages.Count == 0)
            {
                messages.Add(message);
            }
            else
            {
                int index = messages.Count - 1;
                var comp = messages[index];

                while(comp.ticks > messages[index].ticks && index > 0)
                    index--;

                messages.Insert(index, message);
            }

            OnMessageAdded?.Invoke(message);
        }

        protected override string GenerateHttpInitializationResponseBody(string query)
        {
            Debug.Assert(string.IsNullOrEmpty(query));

            return JsonConvert.SerializeObject(messages);
        }
    }
}
