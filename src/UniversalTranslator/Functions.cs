using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;
using System.Text.Json;

namespace UniversalTranslator;

public record User(string GroupName, string UserId, string Language, string? ConnectionId);
public record UserMessage(string GroupName, string SourceUserId, string TargetUserId, string Message);

public class SendMessageOutput
{
    [SignalROutput(HubName = "chat")]
    public SignalRMessageAction? SignalRMessage { get; set; }
    
    [HttpResult]
    public HttpResponseData? HttpResponse { get; set; }
}

public class Functions
{
    private readonly StorageService _storageService;
    private readonly TranslationService _translationService;

    public Functions(StorageService storageService, TranslationService translationService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
    }

    [Function("SendMessageToUser")]
    public async Task<SendMessageOutput> SendMessageToUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(body))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Request body cannot be empty.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            var user = JsonSerializer.Deserialize<UserMessage>(body);

            if (!user.IsValid())
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid user data provided. GroupName, SourceUserId, TargetUserId, and Message are required.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            var chatMembers = await _storageService.GetChatMembersAsync(user.GroupName);

            if (chatMembers is null || chatMembers.Count < 1)
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await errorResponse.WriteStringAsync("No chat members found for the specified group.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            if (!chatMembers.ContainsKey(user.SourceUserId))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await errorResponse.WriteStringAsync($"Source user '{user.SourceUserId}' not found in group '{user.GroupName}'.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            if (!chatMembers.ContainsKey(user.TargetUserId))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await errorResponse.WriteStringAsync($"Target user '{user.TargetUserId}' not found in group '{user.GroupName}'.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            var sourceLanguage = chatMembers[user.SourceUserId].Language;
            var targetLanguage = chatMembers[user.TargetUserId].Language;

            // If source and target languages are the same, no translation is needed
            var translatedText = sourceLanguage == targetLanguage
            ? user.Message
            : await _translationService.TranslateAsync(user.Message, sourceLanguage, targetLanguage);

            // Create the response data
            var responseData = new
            {
                OriginalText = user.Message,
                TranslatedText = translatedText,
                SourceUserId = user.SourceUserId,
                TargetUserId = user.TargetUserId,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                GroupName = user.GroupName
            };

            var httpResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            httpResponse.Headers.Add("Content-Type", "application/json");
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(responseData));

            var signalRMessage = new SignalRMessageAction("newMessage")
            {
                GroupName = user.GroupName,
                UserId = user.TargetUserId,
                Arguments = [responseData]
            };

            return new SendMessageOutput 
            { 
                HttpResponse = httpResponse,
                SignalRMessage = signalRMessage
            };
        }
        catch (JsonException ex)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync($"Invalid JSON format in request body: {ex.Message}");
            return new SendMessageOutput { HttpResponse = errorResponse };
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while processing the message: {ex.Message}");
            return new SendMessageOutput { HttpResponse = errorResponse };
        }
    }

    private bool TryGetChatMember(string groupName, out IDictionary<string, ChatMember> chatMembers, out HttpResponseData response)
    {
        chatMembers = _storageService.GetChatMembersAsync(groupName).GetAwaiter().GetResult();
        
        if (chatMembers is null || chatMembers.Count < 1)
        {
            response = new HttpResponseData(System.Net.HttpStatusCode.NotFound);
            response.WriteStringAsync("No chat members found for the specified group.").GetAwaiter().GetResult();
            return false;
        }

        response = null;
        return true;
    }

    [Function("AddChatMember")]
    public async Task<HttpResponseData> AddChatMember(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            var user = JsonSerializer.Deserialize<User>(requestBody);

            if (user is null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request body format.");
                return badResponse;
            }

            // Validate required properties after deserialization
            if (string.IsNullOrWhiteSpace(user.GroupName))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("GroupName is required.");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(user.UserId))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("UserId is required.");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(user.Language))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Language is required.");
                return badResponse;
            }

            // Check if user already exists in the group
            var existingMembers = await _storageService.GetChatMembersAsync(user.GroupName);
            if (existingMembers != null && existingMembers.ContainsKey(user.UserId))
            {
                var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync($"User '{user.UserId}' already exists in group '{user.GroupName}'.");
                return conflictResponse;
            }

            var member = new ChatMember(user.GroupName, user.UserId, user.Language);

            await _storageService.AddChatMemberAsync(member);

            var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await response.WriteStringAsync("Chat member added successfully.");
            return response;
        }
        catch (JsonException ex)
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync($"Invalid JSON format: {ex.Message}");
            return badResponse;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
            await conflictResponse.WriteStringAsync("User already exists in the group.");
            return conflictResponse;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while adding the chat member: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("RemoveChatMember")]
    public async Task<HttpResponseData> RemoveChatMember(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "removechatmember/{groupName}/{userId}")] HttpRequestData req,
        string groupName,
        string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("GroupName is required.");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("UserId is required.");
                return badResponse;
            }

            await _storageService.RemoveChatMemberAsync(groupName, userId);
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Chat member removed successfully.");
            return response;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Chat member '{userId}' not found in group '{groupName}'.");
            return notFound;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while removing the chat member: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetChatMembers")]
    public async Task<HttpResponseData> GetChatMembers(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getchatmembers/{groupName}")] HttpRequestData req,
        string groupName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("GroupName is required.");
                return badResponse;
            }

            var members = await _storageService.GetChatMembersAsync(groupName);
            
            if (members == null)
            {
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"No chat members found for group '{groupName}'.");
                return notFoundResponse;
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(members));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while retrieving chat members: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("IsUserNameAvailable")]
    public async Task<HttpResponseData> IsUserNameAvailable(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "isusernameavailable/{username}")] HttpRequestData req,
        string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Username is required.");
                return badResponse;
            }

            // Basic username validation
            if (username.Length < 2 || username.Length > 50)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Username must be between 2 and 50 characters.");
                return badResponse;
            }

            var isAvailable = await _storageService.IsUserNameAvailableAsync(username);
            
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { IsAvailable = isAvailable }));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while checking username availability: {ex.Message}");            return errorResponse;
        }
    }
}
