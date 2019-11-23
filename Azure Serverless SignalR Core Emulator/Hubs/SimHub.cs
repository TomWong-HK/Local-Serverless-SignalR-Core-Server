using Azure_Serverless_SignalR_Core_Emulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Azure_Serverless_SignalR_Core_Emulator.Hubs
{
    [Authorize]
    public class SimHub : Hub
    {
        private readonly MappingService mappingService;
        public SimHub(MappingService mappingService)
        {
            this.mappingService = mappingService;
        }
        public override async Task OnConnectedAsync()
        {
            string targetedHub = Context.GetHttpContext().Request.Query["hub"];
            if (string.IsNullOrEmpty(targetedHub))
            {
                Context.Abort();
                return;
            }
            string userId = Context.User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            mappingService.AddConnection(Context.ConnectionId, targetedHub, userId);
            await base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            mappingService.RemoveConnection(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
