using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

namespace UniversalTranslator;

public class Functions
{
    private readonly ILogger _logger;

    public Functions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Functions>();
    }

    [Function(nameof(BroadcastToAll))]
    [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
    public static SignalRMessageAction BroadcastToAll([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        using var bodyReader = new StreamReader(req.Body);
        return new SignalRMessageAction("newMessage")
        {
            // broadcast to all the connected clients without specifying any connection, user or group.
            Arguments = new[] { bodyReader.ReadToEnd() },
        };
    }

    [Function(nameof(SendToUser))]
    [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
    public static SignalRMessageAction SendToUser([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        using var bodyReader = new StreamReader(req.Body);
        return new SignalRMessageAction("newMessage")
        {
            Arguments = new[] { bodyReader.ReadToEnd() },
            UserId = "userToSend",
        };
    }

    [Function(nameof(SendToGroup))]
    [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
    public static SignalRMessageAction SendToGroup([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        using var bodyReader = new StreamReader(req.Body);
        return new SignalRMessageAction("newMessage")
        {
            Arguments = new[] { bodyReader.ReadToEnd() },
            GroupName = "groupToSend"
        };
    }

    [Function(nameof(RemoveFromGroup))]
    [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
    public static SignalRGroupAction RemoveFromGroup([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        return new SignalRGroupAction(SignalRGroupActionType.Remove)
        {
            GroupName = "group1",
            UserId = "user1"
        };
    }

    [Function(nameof(RemoveFromGroup))]
    [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
    public static SignalRGroupAction AddToGroupGroup([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        return new SignalRGroupAction(SignalRGroupActionType.Add)
        {
            GroupName = "group1",
            UserId = "user1"
        };
    }
}

