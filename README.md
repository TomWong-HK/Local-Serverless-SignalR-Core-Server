# Local-Serverless-SignalR-Core-Server
An ASP .NET Core Server that emulates a serverless Azure SignalR service.

Only SignalR Core is supported, although Azure SignalR supports both.

All V1 REST API documented in [Microsoft Docs](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-quickstart-rest-api#usage) has been implemented. 

# Usage
## Server
1. Edit the "SecretKey" value in appsettings.json.
The "SecretKey" acts the same as the [access key for Azure SignalR Service](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-howto-key-rotation). It is used for authentication in SignalR connection and REST API requests.

2. Build and launch the webserver.
   
   For the binary, the argument `--urls` can be used to specify the listening IP and port. By default, http://localhost:5000 & https://localhost:5001 will be listened. For example, 
```
"Local Serverless SignalR Core.exe" --url=http://127.0.0.1:7777
"Local Serverless SignalR Core.exe" --url=http://127.0.0.1:7777;https://127.0.0.1:7778
```

## Negotiation
The tutorial in [Microsoft Docs](https://docs.microsoft.com/en-US/azure/azure-signalr/signalr-quickstart-azure-functions-csharp) uses the Azure Functions to build an endpoint for the SignalR client to connect. We have to implement this as well.

In fact, when a SignalR client connects to an endpoint, it will first make a negotiate request to `endpoint url + '/negotiate'`. It is why one of the Azure Function is named `negotiate`. The Auzre Function will then return an endpoint of Azure SignalR service and a JWT token for the connection. Finally, the client will use the token and negotiate again with Azure SignalR service. 

Therefore, simply create a POST API with path ended with `/negotiate` and return the following JSON.
```
{
     "url": "http://localhost:5000/client/?hub=chat"
     "accessToken": <A JWT Token>
}
```
1. Originally, `url` will be the endpoint of the Azure SignalR (`https://xxxxx.service.signalr.net/client/?hub=chat`). Now, we edit it to the one for the ASP server. The query `hub=chat` controls the hub that the client will join.

2. `accessToken` is built by setting the `url` value as audience claim and signed by the "SecretKey". Other claims are not necessary.

   To specify an userId, add an extra claim named `nameid` for that. It may be useful when dealing with some [REST API](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-quickstart-rest-api#usage), like `Sending to specific users`, `Adding a user to a group`, `Removing a user from a group`.

