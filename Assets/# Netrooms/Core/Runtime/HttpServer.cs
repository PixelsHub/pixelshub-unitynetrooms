using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PixelsHub.Netrooms.Server
{
    public delegate Task HttpServerRequestHandler(HttpListenerRequest req, HttpListenerResponse resp, string query);

    public class HttpServer : MonoBehaviour
    {
        private struct RequestParse
        {
            public string endpoint;
            public string query;
        }

        public bool IsServerListening { get; private set; }

        public string Port
        {
            get => port;
            set 
            {
                if(enabled)
                    throw new InvalidOperationException("Port cannot be changed in an enabled server.");
                
                if(string.IsNullOrEmpty(value))
                    throw new ArgumentException("Port cannot be set to a null or empty value.");

                port = value;
            }
        }

        private HttpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        [SerializeField]
        private string port = "8080";

        private readonly Dictionary<string, Dictionary<string, HttpServerRequestHandler>> handlers = new();
        
        public void AddHandler(string method, string endpoint, HttpServerRequestHandler handler)
        {
            method = ValidateMethod(method);

            if(handlers.TryGetValue(method, out var dictionary))
            {
                if(dictionary.ContainsKey(endpoint))
                {
                    dictionary[endpoint] = handler;
                    Debug.LogWarning($"Switching current endpoint handler at \"{endpoint}\".");
                }
                else
                    dictionary.Add(endpoint, handler);
            }
            else
                handlers.Add(method, new() { { endpoint, handler } });
        }

        public bool RemoveHandler(string method, string endpoint)
        {
            method = ValidateMethod(method);

            if(handlers.TryGetValue(method, out var dictionary))
                return dictionary.Remove(endpoint);

            return false;
        }

        protected virtual void OnDestroy() 
        {
            cancellationTokenSource?.Dispose();
        }

        private static string ValidateMethod(string method) => method.Trim().ToUpper();

        protected void StartServerListening() 
        {
            if(IsServerListening)
            {
                Debug.Assert(false);
                return;
            }

            listener = new HttpListener();
            string prefix = $"http://*:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();

            cancellationTokenSource = new();
            cancellationToken = cancellationTokenSource.Token;

            IsServerListening = true;

            Task.Run(ListenToConnectionsAsync, cancellationTokenSource.Token);

            Debug.Log($"Started http server at {prefix}");
        }

        protected void StopServerListening() 
        {
            if(IsServerListening)
            {
                IsServerListening = false;

                cancellationTokenSource.Cancel();
                listener.Close();

                Debug.Log("Stopped http server.");
            }
        }

        private async Task ListenToConnectionsAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();

            while(IsServerListening)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();

                    cancellationToken.ThrowIfCancellationRequested();

                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse resp = context.Response;

                    if(handlers.TryGetValue(request.HttpMethod, out var dictionary))
                    {
                        var parse = ParseUrlPath(request.Url.LocalPath);

                        if(dictionary.TryGetValue(parse.endpoint, out var handler))
                        {
                            var task = handler.Invoke(request, resp, parse.query);

                            if(task != null)
                                await task;
                        }
                        else
                            resp.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    else
                    {
                        resp.StatusCode = (int)HttpStatusCode.BadRequest;
                        Debug.LogError("Received request with unsupported method: " + request.HttpMethod);
                    }

                    resp.Close();

                    if(cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();
                }
                catch(ObjectDisposedException ex)
                {
                    if(ex.ObjectName != typeof(HttpListener).ToString())
                        Debug.LogError(ex);

                    break;
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }

        private static RequestParse ParseUrlPath(string path)
        {
            RequestParse parse = new();

            path = path.Trim('/');

            int i = 0;
            for(; i < path.Length; i++)
            {
                if(path[i] == '/')
                {
                    parse.endpoint = path[..i];
                    i++; // Increment to set query (if present) without initial slash
                    break;
                }
            }

            parse.endpoint ??= path;

            string query = string.Empty;
            if(i < path.Length)
                query = path[i..];

            parse.query = query;

            return parse;
        }
    }
}