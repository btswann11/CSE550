using Azure;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit.Sdk;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UniversalTranslator.UnitTests;

public class FunctionUnitTests
{
    private Mock<IStorageService> _storageServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<ILogger<Functions>> _loggerMock;
    private Mock<FunctionContext> _functionContext;

    public FunctionUnitTests()
    {
        _storageServiceMock = new Mock<IStorageService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _loggerMock = new Mock<ILogger<Functions>>();
        _functionContext = new Mock<FunctionContext>();
    }

    [Fact]
    public async Task GetIndex_ShouldReturnOk()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.GetIndex(request);
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSupportedLanguages_Success_ReturnsOkWithLanguages()
    {
        // Arrange
        var expectedLanguages = """{"translation":{"en":{"name":"English"},"es":{"name":"Spanish"}}}""";
        _translationServiceMock.Setup(x => x.GetSupportedLanguagesAsync())
            .ReturnsAsync(expectedLanguages);

        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));


        // Act
        var response = await function.GetSupportedLanguages(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _translationServiceMock.Verify(x => x.GetSupportedLanguagesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSupportLanguages_Failure_ReturnsInternalServerError()
    {
        // Arrange
        _translationServiceMock.Setup(x => x.GetSupportedLanguagesAsync())
            .ThrowsAsync(new Exception("Service error"));
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));
        // Act
        var response = await function.GetSupportedLanguages(request);
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        _translationServiceMock.Verify(x => x.GetSupportedLanguagesAsync(), Times.Once);
    }

    #region SendMessageToUser Tests

    [Fact]
    public async Task SendMessageToUser_EmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));
        // Act
        var response = await function.SendMessageToUser(request);
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpResponse.StatusCode);
    }

    [Theory]
    [InlineData("", "sourceUserId", "targetUserId", "message")]
    [InlineData(null, "sourceUserId", "targetUserId", "message")]
    [InlineData("groupName", "", "targetUserId", "message")]
    [InlineData("groupName", null, "targetUserId", "message")]
    [InlineData("groupName", "sourceUserId", "", "message")]
    [InlineData("groupName", "sourceUserId", null, "message")]
    [InlineData("groupName", "sourceUserId", "targetUserId", "")]
    [InlineData("groupName", "sourceUserId", "targetUserId", null)]
    public async Task SendMessageToUser_EmptyUserMessageProperties_ReturnsBadRequest(string groupName, string sourceUserId, string targetUserId, string message)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userMessage = new UserMessage(groupName, sourceUserId, targetUserId, message, DateTime.Now);
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userMessage)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        // Act
        var response = await function.SendMessageToUser(request);
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpResponse.StatusCode);
    }

    [Fact]
    public async Task SendMessage_NoChatMembersForGroupName_ReturnsNotFound()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userMessage = new UserMessage("groupName", "sourceUserId", "targetUserId", "message", DateTime.Now);
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userMessage)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);
        _storageServiceMock.Setup(x => x.GetChatMembersAsync("groupName"))
            .ReturnsAsync(new Dictionary<string, ChatMember>());
        // Act
        var response = await function.SendMessageToUser(request);
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.HttpResponse.StatusCode);
    }

    [Theory]
    [InlineData("sourceUserId", null)]
    [InlineData(null, "targetUserId")]
    public async Task SendMessage_ChatMembersDictionaryDoesNotContainUserId_ReturnsNotFound(string sourceUserId, string targetUserId)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userMessage = new UserMessage("groupName", sourceUserId ?? "failing", targetUserId ?? "failing", "message", DateTime.Now);
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userMessage)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        // if chat members does not contain sourceUserId it will return NotFound
        var chatMembers = new Dictionary<string, ChatMember>
        {
            { sourceUserId == null ? targetUserId : sourceUserId, new ChatMember("groupName", sourceUserId == null ? targetUserId : sourceUserId, "en", "connectionId") }
        };

        _storageServiceMock.Setup(x => x.GetChatMembersAsync("groupName"))
            .ReturnsAsync(chatMembers);

        // Act
        var response = await function.SendMessageToUser(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.HttpResponse.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userMessage = new UserMessage("groupName", "sourceUserId", "targetUserId", "message", DateTime.Now);
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userMessage)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        _storageServiceMock.Setup(x => x.GetChatMembersAsync("groupName"))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var response = await function.SendMessageToUser(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.HttpResponse.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ValidRequest_ReturnsOk()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userMessage = new UserMessage("groupName", "sourceUserId", "targetUserId", "message", DateTime.Now);
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userMessage)));


        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        var chatMembers = new Dictionary<string, ChatMember>
        {
            { "sourceUserId", new ChatMember("groupName", "sourceUserId", "en", "connectionId") },
            { "targetUserId", new ChatMember("groupName", "targetUserId", "en", "connectionId") }
        };
        _storageServiceMock.Setup(x => x.GetChatMembersAsync("groupName"))
            .ReturnsAsync(chatMembers);
        // Act
        var response = await function.SendMessageToUser(request);
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.HttpResponse.StatusCode);
    }

    #endregion SendMessageToUser Tests

    #region AddChatMember Tests

    [Fact]
    public async Task AddChatMember_EmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.AddChatMember(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "userId", "en", "connectionId")]
    [InlineData("  ", "userId", "en", "connectionId")]
    [InlineData("groupName", "", "en", "connectionId")]
    [InlineData("groupName", "  ", "en", "connectionId")]
    [InlineData("groupName", "userId", "", "connectionId")]
    [InlineData("groupName", "userId", "  ", "connectionId")]
    [InlineData("groupName", "userId", "en", "")]
    [InlineData("groupName", "userId", "en", "  ")]
    public void ChatMember_EmptyStringProperties_ThrowsArgumentException(string groupName, string userId, string language, string connectionId)
    {
        Assert.Throws<ArgumentException>(() => new ChatMember(groupName, userId, language, connectionId));
    }

    [Theory]
    [InlineData(null, "userId", "en", "connectionId")]
    [InlineData("groupName", null, "en", "connectionId")]
    [InlineData("groupName", "userId", null, "connectionId")]
    [InlineData("groupName", "userId", "en", null)]
    public void ChatMember_NullProperties_ThrowsArgumentNullException(string groupName, string userId, string language, string connectionId)
    {
        Assert.Throws<ArgumentNullException>(() => new ChatMember(groupName, userId, language, connectionId));
    }

    [Fact]
    public async Task AddChatMember_IfUserAlreadyInGroup_ReturnsConflict()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var chatMember = new ChatMember("groupName", "userId", "en", "connectionId");
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(chatMember)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);
        _storageServiceMock.Setup(x => x.GetChatMembersAsync(chatMember.GroupName))
            .ReturnsAsync(new Dictionary<string, ChatMember>
            {
                { chatMember.RowKey, chatMember }
            });
        // Act
        var response = await function.AddChatMember(request);
        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddChatMember_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var chatMember = new ChatMember("groupName", "userId", "en", "connectionId");
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(chatMember)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(chatMember.GroupName))
            .ReturnsAsync(new Dictionary<string, ChatMember>());

        // Act
        var response = await function.AddChatMember(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddChatMember_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var chatMember = new ChatMember("groupName", "userId", "en", "connectionId");
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(chatMember)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(chatMember.GroupName))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var response = await function.AddChatMember(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
    #endregion AddChatMember Tests

    #region RemoveChatMember Tests

    [Theory]
    [InlineData("", "userId")]
    [InlineData("  ", "userId")]
    [InlineData(null, "userId")]
    [InlineData("groupName", "")]
    [InlineData("groupName", "  ")]
    [InlineData("groupName", null)]
    public async Task RemoveChatMember_InvalidParameters_ReturnsBadRequest(string groupName, string userId)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.RemoveChatMember(request, groupName, userId);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveChatMembers_HasNonEmptyParameters_ReturnsOk()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var groupName = "groupName";
        var userId = "userId";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Setup the storage service to return a chat member
        _storageServiceMock.Setup(x => x.RemoveChatMemberAsync(groupName, userId))
            .Returns(Task.CompletedTask);

        // Act
        var response = await function.RemoveChatMember(request, groupName, userId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    #endregion RemoveChatMember Tests

    #region GetChatMembers Tests
    [Fact]
    public async Task GetChatMembers_EmptyGroupName_ReturnsBadRequest()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.GetChatMembers(request, "");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetChatMembers_EmptyOrNullGroup_ReturnsNotFound(bool testNull)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var groupName = "groupName";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(groupName))
            .ReturnsAsync(testNull ? null : new Dictionary<string, ChatMember>());

        // Act
        var response = await function.GetChatMembers(request, groupName);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetChatMembers_ValidGroupName_ReturnsOkWithMembers()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var groupName = "groupName";
        var chatMembers = new Dictionary<string, ChatMember>
        {
            { "userId1", new ChatMember(groupName, "userId1", "en", "connectionId1") },
            { "userId2", new ChatMember(groupName, "userId2", "en", "connectionId2") }
        };

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(groupName))
            .ReturnsAsync(chatMembers);

        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.GetChatMembers(request, groupName) as FakeHttpResponseData;
        // Deserialize the response body to a dictionary
        var body = response.GetBodyAs<Dictionary<string, ChatMember>>();
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(chatMembers.Count, body.Count);
    }

    #endregion GetChatMembers Tests

    #region IsUserOnline Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsUserOnline_EmptyOrNullUserName_ReturnsBadRequest(bool testNull)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userId = testNull ? null : "";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.IsUserOnline(request, userId);
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public record UserOnlineResponse(bool IsOnline);
    [Fact]
    public async Task IsUserOnline_UserFound_ReturnsOk()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userId = "userId";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        _storageServiceMock.Setup(x => x.IsUserOnlineAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        // Act
        var response = await function.IsUserOnline(request, userId) as FakeHttpResponseData;
        var body = response.GetBodyAs<UserOnlineResponse>();
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.IsOnline);
    }

    [Fact]
    public async Task IsUserOnline_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userId = "userId";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        _storageServiceMock.Setup(x => x.IsUserOnlineAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var response = await function.IsUserOnline(request, userId);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion IsUserOnline Tests

    #region DeleteUser Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteUser_EmptyOrNullUserName_ReturnsBadRequest(bool testNull)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userId = testNull ? null : "";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));
        // Act
        var response = await function.DeleteUser(request, userId);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_UserFound_ReturnsOk()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userId = "userId";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        _storageServiceMock.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await function.DeleteUser(request, userId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var userId = "userId";
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        _storageServiceMock.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var response = await function.DeleteUser(request, userId);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion DeleteUser Tests
    #region CreateUserProfile Tests

    [Fact]
    public async Task CreateUserProfile_EmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.CreateUserProfile(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserProfile_InvalidBodyStructure_ReturnsBadRequest()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var invalidBody = new
        {
            Name = "John Doe",
            Email = ""
        };
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(invalidBody)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        // Act
        var response = await function.CreateUserProfile(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserProfile_UserIdAlreadyExists_ReturnsConflict()
    {
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);

        var existingUser = new User("groupName", "existingUserId", "en", "connectionId");
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(existingUser)));

        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, ChatMember>
            {
                { "existingUserId", new ChatMember("groupName", "existingUserId", "en", "connectionId") }
            });

        var response = await function.CreateUserProfile(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserProfile_ValidRequest_ReturnsCreated()
    {
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var newUser = new User("groupName", "newUserId", "en", "connectionId");
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newUser)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);
        _storageServiceMock.Setup(x => x.GetChatMembersAsync(It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, ChatMember>());
        _storageServiceMock.Setup(x => x.AddChatMemberAsync(It.IsAny<ChatMember>()));

        // Act
        var response = await function.CreateUserProfile(request);
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserProfile_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var newUser = new User("groupName", "newUserId", "en", "connectionId");
        var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newUser)));
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"), body);

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var response = await function.CreateUserProfile(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
    #endregion CreateUserProfile Tests

    #region CheckProfile Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckProfile_EmptyUserName_ReturnsBadRequest(bool testNull)
    {
        // Arrange
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        // Act
        var response = await function.CheckProfile(request, testNull ? null : "");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public record CheckProfileResponse(bool UserExists);
    
    [Fact]
    public async Task CheckProfile_UserExists_ReturnsTrueWithOk()
    {
        // Arrange
        string userId = "userId";
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));

        _storageServiceMock.Setup(x => x.GetChatMembersAsync(It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, ChatMember>
            {
                { userId, new ChatMember("groupName", "user", "en", "connectionId") }
            });

        // Act
        var response = await function.CheckProfile(request, userId) as FakeHttpResponseData;
        var body = response.GetBodyAs<CheckProfileResponse>();
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.UserExists);
    }

    [Fact]
    public async Task CheckProfile_UserDoesNotExist_ReturnsFalseWithOk()
    {
        // Arrange
        string userId = "userId";
        var function = new Functions(_storageServiceMock.Object, _translationServiceMock.Object, _loggerMock.Object);
        var request = new FakeHttpRequestData(_functionContext.Object, new Uri("http://localhost/api/index"));
        _storageServiceMock.Setup(x => x.GetChatMembersAsync(It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, ChatMember>());
        // Act
        var response = await function.CheckProfile(request, userId) as FakeHttpResponseData;
        var body = response.GetBodyAs<CheckProfileResponse>();
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.UserExists);
    }
    #endregion CheckProfile Tests
}

[ExcludeFromCodeCoverage]
public class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext, Uri url, Stream body = null) : base(functionContext)
    {
        Url = url;
        Body = body ?? new MemoryStream();
    }
    public override Stream Body { get; } = new MemoryStream();
    public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    public override Uri Url { get; }
    public override IEnumerable<ClaimsIdentity> Identities { get; }
    public override string Method { get; }
    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(FunctionContext);
    }
}

[ExcludeFromCodeCoverage]
public class FakeHttpResponseData : HttpResponseData
{
    private readonly FunctionContext _functionContext;
    private readonly MemoryStream _body = new MemoryStream();

    public FakeHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
        var mockObjectSerializer = new Mock<ObjectSerializer>();

        // Setup the SerializeAsync method
        mockObjectSerializer.Setup(s => s.SerializeAsync(It.IsAny<Stream>(), It.IsAny<object?>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, object?, Type, CancellationToken>(async (stream, value, type, token) =>
            {
                await System.Text.Json.JsonSerializer.SerializeAsync(stream, value, type, cancellationToken: token);
            });

        _functionContext = functionContext;

        // Initialize the service provider with required services including the serializer
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddOptions();

        // Add the Azure Functions Worker core services including serialization
        services.Configure<Microsoft.Azure.Functions.Worker.WorkerOptions>(options =>
        {
            options.Serializer = mockObjectSerializer.Object;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Mock the FunctionContext to return the service provider
        var contextMock = Mock.Get(functionContext);
        contextMock.Setup(c => c.InstanceServices).Returns(serviceProvider);
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
    public override Stream Body
    {
        get => _body;
        set => throw new NotSupportedException("Cannot set Body directly");
    }
    public override HttpCookies Cookies { get; }

    // Add this helper method to retrieve the body content
    public string GetBodyAsString()
    {
        _body.Position = 0;
        using var reader = new StreamReader(_body);
        return reader.ReadToEnd();
    }

    // Add this helper method to retrieve the body as a specific type
    public T GetBodyAs<T>()
    {
        var json = GetBodyAsString();
        return JsonSerializer.Deserialize<T>(json);
    }

}