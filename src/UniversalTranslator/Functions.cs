using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace UniversalTranslator;

public record User(string GroupName, string UserId, string Language, string? ConnectionId);
public record UserMessage(string GroupName, string SourceUserId, string TargetUserId, string Message);

public class Functions
{
    private readonly StorageService _storageService;
    private readonly TranslationService _translationService;

    public Functions(StorageService storageService, TranslationService translationService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
    }

    [Function("SendToUser")]
    public async Task<SignalRMessageAction> SendToUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var user = JsonSerializer.Deserialize<UserMessage>(body);

        if (user is null || string.IsNullOrWhiteSpace(user.GroupName) || string.IsNullOrWhiteSpace(user.SourceUserId) || string.IsNullOrWhiteSpace(user.TargetUserId))
        {
            throw new ArgumentException("Invalid user data provided.");
        }

        var chatMembers = await _storageService.GetChatMembersAsync(user.GroupName);

        if(chatMembers is null || chatMembers.Count < 1)
        {
            throw new InvalidOperationException("No chat members found for the specified group.");
        }

        var sourceLanguage = chatMembers[user.SourceUserId].Language;
        var targetLanguage = chatMembers[user.TargetUserId].Language;

        // translate message
        var translatedText = await _translationService.TranslateAsync(user.Message, sourceLanguage, targetLanguage);

        return new SignalRMessageAction("newMessage")
        {
            GroupName = user.GroupName,
            UserId = user.TargetUserId,
            Arguments = [translatedText]
        };
    }

    [Function("AddChatMember")]
    public async Task<HttpResponseData> AddChatMember(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var member = JsonSerializer.Deserialize<ChatMember>(requestBody);

        if (member is null)
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid request body.");
            return badResponse;
        }

        member.PartitionKey ??= "default";
        member.RowKey ??= Guid.NewGuid().ToString();

        await _storageService.AddChatMemberAsync(member);

        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteStringAsync("Chat member added.");
        return response;
    }

    [Function("RemoveChatMember")]
    public async Task<HttpResponseData> RemoveChatMember(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "removechatmember/{groupName}/{userId}")] HttpRequestData req,
        string groupName,
        string userId)
    {
        try
        {
            await _storageService.RemoveChatMemberAsync(groupName, userId);
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Chat member removed.");
            return response;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("Chat member not found.");
            return notFound;
        }
    }

    [Function("GetChatMembers")]
    public async Task<HttpResponseData> GetChatMembers(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getchatmembers/{groupName}")] HttpRequestData req,
        string groupName)
    {
        var members = _storageService.GetChatMembersAsync(groupName);
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync(JsonSerializer.Serialize(members));
        return response;
    }
}

