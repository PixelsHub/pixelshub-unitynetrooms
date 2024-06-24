using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class HttpReplicator : IDisposable
    {
        public struct Result
        {
            public string query;
            public HttpContent content;

            public Result(string query, HttpContent content) 
            {
                this.query = query;
                this.content = content;
            }
        }

        private class ClientCache
        {
            public HttpClient client;
            public int references;
        }

        public event Action<Result> OnReplicationSuccessfullyRetrieved;

        public float batchWaitTime = 0.5f;

        private static readonly Dictionary<string, ClientCache> httpClientsCache = new();

        private readonly HttpClient httpClient;
        private readonly StringBuilder updateQueryBuilder = new();

        private readonly string baseAddress;
        private readonly string path;

        private IEnumerator updateBatcher;

        public HttpReplicator(string host, string port, string path)
        {
            baseAddress = $"http://{host}:{port}/";

            if(httpClientsCache.TryGetValue(baseAddress, out var cachedClient))
            {
                httpClient = cachedClient.client;
                cachedClient.references++;
            }
            else
            {
                httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(baseAddress)
                };

                httpClientsCache.Add(baseAddress, new() { client = httpClient, references = 1 });
            }

            this.path = path.Trim(' ', '/');

            ReplicateAll();
        }

        public void Dispose()
        {
            if(httpClientsCache.TryGetValue(baseAddress, out var cachedClient))
            {
                cachedClient.references--;
                if(cachedClient.references <= 0)
                {
                    httpClient.Dispose();
                    httpClientsCache.Remove(baseAddress);
                }
            }

            OnReplicationSuccessfullyRetrieved = null;
        }

        public void Update(string key)
        {
            Debug.Assert(!key.Contains('/'), $"Invalid character '/' found in key \"{key}\"");

            updateQueryBuilder.Append(key).Append(";");

            if(updateBatcher == null)
            {
                updateBatcher = UpdateBatcher();
                StaticCoroutineRunner.Start(updateBatcher);
            }
        }

        public void ReplicateAll() => ExecuteReplication(string.Empty);

        private async void ExecuteReplication(string query)
        {
            var response = await httpClient.GetAsync($"{path}/{query}");
            if(response.IsSuccessStatusCode)
                OnReplicationSuccessfullyRetrieved?.Invoke(new(query, response.Content));
            else
                Debug.LogError($"({response.StatusCode}) {await response.Content.ReadAsStringAsync()}.");
        }

        private IEnumerator UpdateBatcher()
        {
            yield return new WaitForSecondsRealtime(batchWaitTime);

            string query = updateQueryBuilder.ToString();

            if(!string.IsNullOrEmpty(query))
                ExecuteReplication(query);

            updateQueryBuilder.Clear();
            updateBatcher = null;
        }
    }
}