using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.Functions.Worker.SignalRService;

namespace UniversalTranslator;

public record User(string GroupName, string UserId, string Language, string? ConnectionId);
public record UserMessage(string GroupName, string SourceUserId, string TargetUserId, string Message, DateTime TimeStamp);

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
    private readonly ILogger<Functions> _logger;

    public Functions(StorageService storageService, TranslationService translationService, ILogger<Functions> logger, IServiceProvider serviceProvider)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _logger = logger;
    }

    [Function("Index")]
    public async Task<HttpResponseData> Index(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req)
    {
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");

        // Adjust the path as needed for your deployment environment
        var filePath = Path.Combine(Environment.CurrentDirectory, "content", "index.html");

        if (!File.Exists(filePath))
        {
            response.StatusCode = System.Net.HttpStatusCode.NotFound;
            await response.WriteStringAsync("index.html not found.");
            return response;
        }

        var html = await File.ReadAllTextAsync(filePath);
        await response.WriteStringAsync(html);
        return response;
    }

    [Function("GetSupportedLanguages")]
    public async Task<HttpResponseData> GetSupportedLanguages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportedlanguages")] HttpRequestData req)
    {
        try
        {
            var languages = await _translationService.GetSupportedLanguagesAsync();
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(languages);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving supported languages");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while retrieving supported languages: {ex.Message}");
            return errorResponse;
        }
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

            var userMessage = JsonSerializer.Deserialize<UserMessage>(body);

            if (userMessage is null || string.IsNullOrWhiteSpace(userMessage.GroupName) ||
                string.IsNullOrWhiteSpace(userMessage.SourceUserId) ||
                string.IsNullOrWhiteSpace(userMessage.TargetUserId) ||
                string.IsNullOrWhiteSpace(userMessage.Message))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid user data provided. GroupName, SourceUserId, TargetUserId, and Message are required.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            var chatMembers = await _storageService.GetChatMembersAsync(userMessage.GroupName);

            if (chatMembers is null || chatMembers.Count < 1)
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await errorResponse.WriteStringAsync("No chat members found for the specified group.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            if (!chatMembers.ContainsKey(userMessage.SourceUserId))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await errorResponse.WriteStringAsync($"Source user '{userMessage.SourceUserId}' not found in group '{userMessage.GroupName}'.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            if (!chatMembers.ContainsKey(userMessage.TargetUserId))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await errorResponse.WriteStringAsync($"Target user '{userMessage.TargetUserId}' not found in group '{userMessage.GroupName}'.");
                return new SendMessageOutput { HttpResponse = errorResponse };
            }

            var sourceUser = chatMembers[userMessage.SourceUserId];
            var targetUser = chatMembers[userMessage.TargetUserId];

            // If source and target languages are the same, no translation is needed
            var translatedText = sourceUser.Language == targetUser.Language
            ? userMessage.Message
            : await _translationService.TranslateAsync(userMessage.Message, sourceUser.Language, targetUser.Language);

            // Create the response data
            var responseData = new
            {
                OriginalText = userMessage.Message,
                TranslatedText = translatedText,
                SourceUserId = userMessage.SourceUserId,
                TargetUserId = userMessage.TargetUserId,
                SourceLanguage = sourceUser.Language,
                TargetLanguage = targetUser.Language,
                GroupName = userMessage.GroupName,
                TimeStamp = userMessage.TimeStamp
            };

            // Create HTTP response for the sender
            var httpResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(responseData);

            // Create SignalR message for the target user
            var signalRMessage = new SignalRMessageAction("newMessage")
            {
                ConnectionId = targetUser.ConnectionId,
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
            _logger.LogError(ex, "Invalid JSON format in request body for SendMessageToUser");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync($"Invalid JSON format in request body: {ex.Message}");
            return new SendMessageOutput { HttpResponse = errorResponse };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing message in SendMessageToUser");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while processing the message: {ex.Message}");
            return new SendMessageOutput { HttpResponse = errorResponse };
        }
    }

    private bool IsValiderUser(User? user, out string message)
    {
        message = string.Empty;
        if (user is null)
        {
            message = "User data cannot be null.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(user.GroupName))
        {
            message = "GroupName is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(user.UserId))
        {
            message = "UserId is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(user.Language))
        {
            message = "Language is required.";
            return false;
        }
        return true;
    }

    [Function("AddChatMember")]
    public async Task<HttpResponseData> AddChatMember(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
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

            if (!IsValiderUser(user, out var validationMessage))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(validationMessage);
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

            var member = new ChatMember(user.GroupName, user.UserId, user.Language, user.ConnectionId);

            await _storageService.AddChatMemberAsync(member);

            var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await response.WriteStringAsync("Chat member added successfully.");
            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in request body for AddChatMember");
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync($"Invalid JSON format: {ex.Message}");
            return badResponse;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning(ex, "Conflict occurred while adding chat member - user already exists in group");
            var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
            await conflictResponse.WriteStringAsync("User already exists in the group.");
            return conflictResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding chat member");
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
            _logger.LogWarning(ex, "Chat member '{UserId}' not found in group '{GroupName}' for removal", userId, groupName);
            var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Chat member '{userId}' not found in group '{groupName}'.");
            return notFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing chat member '{UserId}' from group '{GroupName}'", userId, groupName);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while removing the chat member: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetChatMembers")]
    public async Task<HttpResponseData> GetChatMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getchatmembers/{groupName}")] HttpRequestData req,
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
            await response.WriteAsJsonAsync(members);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving chat members for group '{GroupName}'", groupName);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while retrieving chat members: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("IsUserOnline")]
    public async Task<HttpResponseData> IsUserOnline(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "isuseronline/{username}")] HttpRequestData req,
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

            var isOnline = await _storageService.IsUserOnlineAsync(username);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { IsOnline = isOnline }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if user '{Username}' is online", username);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while checking username availability: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("DeleteUser")]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deleteuser/{username}")] HttpRequestData req,
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
            // Attempt to delete the user
            await _storageService.DeleteUserAsync(username);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync($"User '{username}' deleted successfully.");
            return response;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning(ex, "User '{Username}' not found for deletion", username);
            var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"User '{username}' not found.");
            return notFoundResponse;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting user '{Username}'", username);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while deleting the user: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("CreateUserProfile")]
    public async Task<HttpResponseData> CreateUserProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "createprofile")] HttpRequestData req)
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

            if (!IsValiderUser(user, out var validationMessage))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(validationMessage);
                return badResponse;
            }

            // Check if user profile already exists by checking if they exist as a chat member with themselves
            var existingMembers = await _storageService.GetChatMembersAsync(user.UserId);
            if (existingMembers != null && existingMembers.ContainsKey(user.UserId))
            {
                var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync($"User profile '{user.UserId}' already exists.");
                return conflictResponse;
            }

            // Create a chat member with themselves to establish the user profile
            var selfChatMember = new ChatMember(user.UserId, user.UserId, user.Language, user.ConnectionId);
            await _storageService.AddChatMemberAsync(selfChatMember);

            var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await response.WriteStringAsync($"User profile '{user.UserId}' created successfully.");
            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in request body for CreateUserProfile");
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync($"Invalid JSON format: {ex.Message}");
            return badResponse;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning(ex, "Conflict occurred while creating user profile - user already exists");
            var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
            await conflictResponse.WriteStringAsync("User profile already exists.");
            return conflictResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user profile");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred while creating the user profile: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("negotiate")]
    public async Task<HttpResponseData> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "chat")] SignalRConnectionInfo connectionInfo)
    {
        _logger.LogInformation("Negotiate endpoint called");
        _logger.LogInformation($"SignalR ConnectionInfo: URL={connectionInfo.Url}, AccessToken exists={!string.IsNullOrEmpty(connectionInfo.AccessToken)}");

        var response = req.CreateResponse(HttpStatusCode.OK);

        // Add CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "*");

        // Log the connection info being returned
        var connectionInfoJson = System.Text.Json.JsonSerializer.Serialize(connectionInfo, options: new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _logger.LogInformation($"Returning connection info: {connectionInfoJson}");

        await response.WriteAsJsonAsync(connectionInfo);
        return response;
    }
}
