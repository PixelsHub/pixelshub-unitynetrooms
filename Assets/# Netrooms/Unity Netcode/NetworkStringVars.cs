using System;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json;

namespace PixelsHub.Netrooms
{
    public class NetworkStringVars : HttpInitializedNetworkBehaviour
    {
        public event Action<string, string> OnVariableSet;

        public override string HttpUrlPath => "stringvars";

        public int Count => variables.Count;

        public IEnumerable<KeyValuePair<string, string>> Variables => variables;

        private Dictionary<string, string> variables = new();

        [SerializeField]
        private bool allowClientChanges = true;

        public void SetVariable(string key, string value)
        {
            if(!allowClientChanges && !IsServer)
            {
                Debug.LogWarning($"Attempted to set variable \"{key}\" but clients are not allowed to set variables.");
                return;
            }    

            if(variables.TryGetValue(key, out string currentValue))
            {
                if(currentValue != value)
                {
                    variables[key] = value;
                    OnVariableSet?.Invoke(key, value);
                    SetVariableNotMeRpc(key, value);
                }
            }
            else
            {
                variables.Add(key, value);
                OnVariableSet?.Invoke(key, value);
                SetVariableNotMeRpc(key, value);
            }
        }

        [Rpc(SendTo.NotMe)]
        private void SetVariableNotMeRpc(string key, string value)
        {
            ApplyLocalSetVariable(key, value);
        }

        private async void ApplyLocalSetVariable(string key, string value)
        {
            while(!isHttpInitializationCompleted)
                await Task.Delay(200);

            if(variables.ContainsKey(key))
                variables[key] = value;
            else
                variables.Add(key, value);

            Debug.Log($"Set var {key} = {value}");
            OnVariableSet?.Invoke(key, value);
        }

        protected override async void ProcessHttpInitialization(HttpContent content)
        {
            Debug.Assert(!IsServer);

            string json = await content.ReadAsStringAsync();
            var replicatedVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            variables = replicatedVariables;
        }

        protected override string GenerateHttpInitializationResponseBody(string query)
        {
            Debug.Assert(string.IsNullOrEmpty(query));

            return JsonConvert.SerializeObject(variables);
        }
    }
}
