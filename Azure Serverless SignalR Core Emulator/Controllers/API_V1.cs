using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure_Serverless_SignalR_Core_Emulator.Hubs;
using Azure_Serverless_SignalR_Core_Emulator.Models;
using Azure_Serverless_SignalR_Core_Emulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Azure_Serverless_SignalR_Core_Emulator.Controllers
{
    [ApiController]
    [Consumes("application/json")]
    [Route("api/v1")]
    [Authorize]
    public class API_V1 : ControllerBase
    {
        private readonly IHubContext<SimHub> hubContext;
        private readonly MappingService mappingService;

        public API_V1(IHubContext<SimHub> hubContext, MappingService mappingService)
        {
            this.hubContext = hubContext;
            this.mappingService = mappingService;
        }
        [HttpPost]
        [Route("hubs/{hubName}")]
        public async Task<ActionResult> BroadcastToEveryOne(string hubName, [FromBody]API_V1Message message)
        {
            return await SendCoreAsync(mappingService.GetConnectionIds(hubName), message);
        }
        [HttpPost]
        [Route("hubs/{hubName}/groups/{groupName}")]
        public async Task<ActionResult> BroadcastToGroup(string hubName, string groupName, [FromBody]API_V1Message message)
        {
            return await SendCoreAsync(mappingService.GetConnectionIdsWithGroup(hubName, groupName), message);
        }
        [HttpPost]
        [Route("hubs/{hubName}/users/{userId}")]
        public async Task<ActionResult> BroadcastToUser(string hubName, string userId, [FromBody]API_V1Message message)
        {
            return await SendCoreAsync(mappingService.GetConnectionIdsWithUserId(hubName, userId), message);
        }

        private async Task<ActionResult> SendCoreAsync(IReadOnlyList<string> connectionIds, API_V1Message message)
        {
            if (connectionIds == null || connectionIds.Count == 0)
                return BadRequest("No matching connection");
            await hubContext.Clients.Clients(connectionIds).SendCoreAsync(message.target, message.arguments);
            return Ok();
        }

        [HttpPut]
        [Route("hubs/{hubName}/groups/{groupName}/users/{userId}")]
        public ActionResult AddUserToGroup(string hubName, string userId, string groupName)
        {
            if (mappingService.AddToGroup(hubName, userId, groupName))
                return Ok();
            return BadRequest("No matching connection");
        }

        [HttpDelete]
        [Route("hubs/{hubName}/groups/{groupName}/users/{userId}")]
        public ActionResult RemoveUserFromGroup(string hubName, string userId, string groupName)
        {
            if (mappingService.RemoveFromGroup(hubName, userId, groupName))
                return Ok();
            return BadRequest("No matching connection");
        }
    }
}
