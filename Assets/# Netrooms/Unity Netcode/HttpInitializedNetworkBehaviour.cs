using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using PixelsHub.Netrooms.Server;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// NetworkBehaviour with a http-based initial synchronization.
    /// </summary>
    public abstract class HttpInitializedNetworkBehaviour : NetworkBehaviour
    {
        public abstract string HttpUrlPath { get; }

        protected HttpServer httpServer;

        protected bool isHttpInitializationCompleted;

        public override void OnNetworkSpawn()
        {
            if(IsServer)
                httpServer.AddHandler(HttpMethod.Get.Method, HttpUrlPath, HttpServerRequestHandler);
            else
                InitializeHttpNetworkClientReplication();
        }

        public override void OnNetworkDespawn()
        {
            if(IsServer)
            {
                httpServer.RemoveHandler(HttpMethod.Get.Method, HttpUrlPath);
            }
        }

        protected virtual void Awake()
        {
            httpServer = FindFirstObjectByType<HttpServer>();
        }

        private async void InitializeHttpNetworkClientReplication()
        {
            if(isHttpInitializationCompleted)
            {
                Debug.Assert(false);
                return;
            }

            using HttpClient httpClient = new();

            string url = $"http://{NetworkUtilities.TransportConnectionAddress}:{httpServer.Port}/{HttpUrlPath}/";
            var response = await httpClient.GetAsync(url);

            if(response.IsSuccessStatusCode)
            {
                ProcessHttpInitialization(response.Content);
                isHttpInitializationCompleted = true;
            }
            else
                Debug.LogError($"({response.StatusCode}) {await response.Content.ReadAsStringAsync()}");
        }

        private async Task HttpServerRequestHandler(HttpListenerRequest request, HttpListenerResponse response, string query)
        {
            var writer = new StreamWriter(response.OutputStream, response.ContentEncoding);

            try
            {
                await writer.WriteAsync(GenerateHttpInitializationResponseBody(query));
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await writer.WriteAsync(ex.ToString());
            }

            writer.Close();
        }

        protected abstract void ProcessHttpInitialization(HttpContent content);

        protected abstract string GenerateHttpInitializationResponseBody(string query);
    }
}