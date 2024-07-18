[Return to main page](../)

# HTTP
Built-in HTTP capabilities for both server and client requirements are provided.\

## HTTP Server
An instance of `HttpServer` creates a simple HTTP listener that can bse used by implementations to define how to handle their own HTTP API.\
The following examples shows how to implement custom API handlers:
```C#
httpServer.AddHandler(HttpMethod.Get.Method, "mypath", RequestHandler);
```
```C#
private async Task RequestHandler(HttpListenerRequest request, HttpListenerResponse response, string query)
{
    var writer = new StreamWriter(response.OutputStream, response.ContentEncoding);
    await writer.WriteAsync(GenerateResponseBody(query)); // Example body
    response.StatusCode = (int)HttpStatusCode.OK;
    writer.Close();
}
```
```C#
httpServer.RemoveHandler(meHttpMethod.Get.Method, "mypath");
```
> [!IMPORTANT]
> An HTTP server derived implementation through `NetworkHttpServer` has already been created to automatically start or stop the listener based on Unity Netcode's network server events.

## Networking HTTP initialization
Some replication use cases can cause errors and exceptions due to the memory size requirements when performing initial synchronizations of connected clients.\
\
For their prevention, the abstract class `HttpInitializedNetworkBehaviour` has been created, 
allowing a behaviour to initialize through "full" replication available by internally handling an HTTP GET method, and then relying on RPC calls for further runtime replication, instead of using network variables for replication.\
The class is used to control **both server and client logic**, and therefore automatically sets up an API handler when spawned in the server.\
\
Specific implementations must:
1. Define the desired url path, to be used by both server (API handler) and client (request URL):
```C#
public override string HttpUrlPath => "mycontent";
```
2. Define how to process the HTTP initialization in a client (serialization example via json):
```C#
protected override async void ProcessHttpInitialization(HttpContent content)
{
    string json = await content.ReadAsStringAsync();
    myLocalContent = JsonConvert.DeserializeObject<List<MyContent>>(json);
}
```
3. Define how to generate the HTTP body for a server response (serialization example via json):
```C#
protected override string GenerateHttpInitializationResponseBody(string query)
{
    return JsonConvert.SerializeObject(myLocalContent); // Send local content from the server
}
```
Any further replication needs are expected to be fullfilled by Rpc calls that send required data among connected instances of the network behaviour.
> [!NOTE]
> There following are currently built-in behaviours implemented as HTTP-initialized:
> - `NetworkStringVars`
> - `NetworkChat`
