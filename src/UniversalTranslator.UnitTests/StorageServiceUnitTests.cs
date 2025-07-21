using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Moq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace UniversalTranslator.UnitTests;

public class StorageServiceUnitTests : IDisposable
{
    private readonly Mock<TableClient> _mockTableClient;
    private readonly StorageService _storageService;

    public StorageServiceUnitTests()
    {
        _mockTableClient = new Mock<TableClient>(MockBehavior.Strict);
        
        // Setup CreateIfNotExists to prevent constructor from failing  
        var mockTableItem = new TableItem("TestTable");
        var mockTableResponse = Response.FromValue(mockTableItem, Mock.Of<Response>());
        _mockTableClient.Setup(x => x.CreateIfNotExists(It.IsAny<CancellationToken>()))
            .Returns(mockTableResponse);

        _storageService = new StorageService(_mockTableClient.Object);
    }

    public void Dispose()
    {
        _mockTableClient?.Reset();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTableClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StorageService(null!));
    }

    [Fact]
    public void Constructor_WithValidTableClient_CallsCreateIfNotExists()
    {
        // Arrange
        var mockTableClient = new Mock<TableClient>(MockBehavior.Strict);
        var mockTableItem = new TableItem("TestTable");
        var mockTableResponse = Response.FromValue(mockTableItem, Mock.Of<Response>());
        mockTableClient.Setup(x => x.CreateIfNotExists(It.IsAny<CancellationToken>()))
            .Returns(mockTableResponse);

        // Act
        var service = new StorageService(mockTableClient.Object);

        // Assert
        mockTableClient.Verify(x => x.CreateIfNotExists(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddChatMemberAsync Tests

    [Fact]
    public async Task AddChatMemberAsync_WithNullMember_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.AddChatMemberAsync(null!));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task AddChatMemberAsync_WithNullPartitionKey_ThrowsArgumentNullException()
    {
        // Arrange
        var member = new ChatMember
        {
            PartitionKey = null!,
            RowKey = "user1",
            UserId = "user1",
            GroupName = "group1",
            Language = "en"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.AddChatMemberAsync(member));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task AddChatMemberAsync_WithEmptyOrWhitespacePartitionKey_ThrowsArgumentException(string partitionKey)
    {
        // Arrange
        var member = new ChatMember
        {
            PartitionKey = partitionKey,
            RowKey = "user1",
            UserId = "user1",
            GroupName = "group1",
            Language = "en"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.AddChatMemberAsync(member));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task AddChatMemberAsync_WithNullRowKey_ThrowsArgumentNullException()
    {
        // Arrange
        var member = new ChatMember
        {
            PartitionKey = "group1",
            RowKey = null!,
            UserId = "user1",
            GroupName = "group1",
            Language = "en"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.AddChatMemberAsync(member));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task AddChatMemberAsync_WithEmptyOrWhitespaceRowKey_ThrowsArgumentException(string rowKey)
    {
        // Arrange
        var member = new ChatMember
        {
            PartitionKey = "group1",
            RowKey = rowKey,
            UserId = "user1",
            GroupName = "group1",
            Language = "en"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.AddChatMemberAsync(member));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task AddChatMemberAsync_WithValidMember_CallsAddEntityAsync()
    {
        // Arrange
        var member = new ChatMember("group1", "user1", "en", "connection1");
        var mockResponse = Mock.Of<Response>();

        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<ChatMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _storageService.AddChatMemberAsync(member);

        // Assert
        _mockTableClient.Verify(x => x.AddEntityAsync(
            It.Is<ChatMember>(m => m.PartitionKey == "group1" && m.RowKey == "user1"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region RemoveChatMemberAsync Tests

    [Fact]
    public async Task RemoveChatMemberAsync_WithNullPartitionKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.RemoveChatMemberAsync(null!, "user1"));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task RemoveChatMemberAsync_WithEmptyOrWhitespacePartitionKey_ThrowsArgumentException(string partitionKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.RemoveChatMemberAsync(partitionKey, "user1"));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task RemoveChatMemberAsync_WithNullRowKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.RemoveChatMemberAsync("group1", null!));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task RemoveChatMemberAsync_WithEmptyOrWhitespaceRowKey_ThrowsArgumentException(string rowKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.RemoveChatMemberAsync("group1", rowKey));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task RemoveChatMemberAsync_WithValidParameters_CallsDeleteEntityAsync()
    {
        // Arrange
        var mockResponse = Mock.Of<Response>();
        _mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<ETag>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _storageService.RemoveChatMemberAsync("group1", "user1");

        // Assert
        _mockTableClient.Verify(x => x.DeleteEntityAsync(
            "group1", 
            "user1", 
            It.IsAny<ETag>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetChatMemberAsync Tests

    [Fact]
    public async Task GetChatMemberAsync_WithNullPartitionKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.GetChatMemberAsync(null!, "user1"));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task GetChatMemberAsync_WithEmptyOrWhitespacePartitionKey_ThrowsArgumentException(string partitionKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.GetChatMemberAsync(partitionKey, "user1"));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task GetChatMemberAsync_WithNullRowKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.GetChatMemberAsync("group1", null!));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task GetChatMemberAsync_WithEmptyOrWhitespaceRowKey_ThrowsArgumentException(string rowKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.GetChatMemberAsync("group1", rowKey));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task GetChatMemberAsync_WithValidParameters_CallsGetEntityAsync()
    {
        // Arrange
        var expectedMember = new ChatMember("group1", "user1", "en", "connection1");
        var mockResponse = Response.FromValue(expectedMember, Mock.Of<Response>());

        _mockTableClient.Setup(x => x.GetEntityAsync<ChatMember>(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _storageService.GetChatMemberAsync("group1", "user1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("group1", result.PartitionKey);
        Assert.Equal("user1", result.RowKey);
        
        _mockTableClient.Verify(x => x.GetEntityAsync<ChatMember>(
            "group1", 
            "user1", 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetChatMembersAsync Tests

    [Fact]
    public async Task GetChatMembersAsync_WithNullPartitionKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.GetChatMembersAsync(null!));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task GetChatMembersAsync_WithEmptyOrWhitespacePartitionKey_ThrowsArgumentException(string partitionKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.GetChatMembersAsync(partitionKey));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task GetChatMembersAsync_WithValidPartitionKey_ReturnsMembers()
    {
        // Arrange
        var members = new List<ChatMember>
        {
            new ChatMember("group1", "user1", "en", "connection1"),
            new ChatMember("group1", "user2", "es", "connection2")
        };

        var mockAsyncPageable = CreateMockAsyncPageable(members);
        _mockTableClient.Setup(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()))
            .Returns(mockAsyncPageable);

        // Act
        var result = await _storageService.GetChatMembersAsync("group1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("user1", result.Keys);
        Assert.Contains("user2", result.Keys);
        Assert.Equal("en", result["user1"].Language);
        Assert.Equal("es", result["user2"].Language);
        
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region IsUserOnlineAsync Tests

    [Fact]
    public async Task IsUserOnlineAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.IsUserOnlineAsync(null!));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task IsUserOnlineAsync_WithEmptyOrWhitespaceUserId_ThrowsArgumentException(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.IsUserOnlineAsync(userId));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task IsUserOnlineAsync_WithUserFound_ReturnsTrue()
    {
        // Arrange
        var members = new List<ChatMember>
        {
            new ChatMember("group1", "user1", "en", "connection1")
        };

        var mockAsyncPageable = CreateMockAsyncPageable(members);
        _mockTableClient.Setup(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()))
            .Returns(mockAsyncPageable);

        // Act
        var result = await _storageService.IsUserOnlineAsync("user1");

        // Assert
        Assert.True(result);
        
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task IsUserOnlineAsync_WithUserNotFound_ReturnsFalse()
    {
        // Arrange
        var members = new List<ChatMember>(); // Empty list

        var mockAsyncPageable = CreateMockAsyncPageable(members);
        _mockTableClient.Setup(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()))
            .Returns(mockAsyncPageable);

        // Act
        var result = await _storageService.IsUserOnlineAsync("user1");

        // Assert
        Assert.False(result);
        
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _storageService.DeleteUserAsync(null!));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task DeleteUserAsync_WithEmptyOrWhitespaceUserId_ThrowsArgumentException(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _storageService.DeleteUserAsync(userId));
        
        // Verify no table operations were called
        VerifyNoTableOperations();
    }

    [Fact]
    public async Task DeleteUserAsync_WithValidUserId_DeletesAllUserEntries()
    {
        // Arrange
        var members = new List<ChatMember>
        {
            new ChatMember("group1", "user1", "en", "connection1"),
            new ChatMember("group2", "user1", "es", "connection2")
        };

        var mockAsyncPageable = CreateMockAsyncPageable(members);
        _mockTableClient.Setup(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()))
            .Returns(mockAsyncPageable);

        var mockResponse = Mock.Of<Response>();
        _mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<ETag>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _storageService.DeleteUserAsync("user1");

        // Assert
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(
            It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
            
        _mockTableClient.Verify(x => x.DeleteEntityAsync(
            "group1", 
            "user1", 
            It.IsAny<ETag>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
            
        _mockTableClient.Verify(x => x.DeleteEntityAsync(
            "group2", 
            "user1", 
            It.IsAny<ETag>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetOnlineUsersAsync Tests

    [Fact]
    public async Task GetOnlineUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        var members = new List<ChatMember>
        {
            new ChatMember("group1", "user1", "en", "connection1"),
            new ChatMember("group1", "user2", "es", "connection2"),
            new ChatMember("group2", "user3", "fr", "connection3")
        };

        var mockAsyncPageable = CreateMockAsyncPageable(members);
        
        // Setup the QueryAsync call without any parameters (as used in GetOnlineUsersAsync)
        _mockTableClient.Setup(x => x.QueryAsync<ChatMember>(
            It.IsAny<string>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()))
            .Returns(mockAsyncPageable);

        // Act
        var result = await _storageService.GetOnlineUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        var resultList = result.ToList();
        Assert.Contains(resultList, m => m.UserId == "user1");
        Assert.Contains(resultList, m => m.UserId == "user2");
        Assert.Contains(resultList, m => m.UserId == "user3");
        
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(
            It.IsAny<string>(), 
            It.IsAny<int?>(), 
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region ChatMember Constructor Tests

    [Fact]
    public void ChatMember_DefaultConstructor_InitializesProperties()
    {
        // Act
        var member = new ChatMember();

        // Assert
        Assert.Equal(string.Empty, member.PartitionKey);
        Assert.Equal(string.Empty, member.RowKey);
        Assert.Equal(string.Empty, member.UserId);
        Assert.Equal(string.Empty, member.GroupName);
        Assert.Equal(string.Empty, member.Language);
        Assert.Equal(string.Empty, member.ConnectionId);
    }

    [Fact]
    public void ChatMember_ParameterizedConstructor_WithValidParameters_InitializesProperties()
    {
        // Arrange
        var groupName = "group1";
        var userId = "user1";
        var language = "en";
        var connectionId = "connection1";

        // Act
        var member = new ChatMember(groupName, userId, language, connectionId);

        // Assert
        Assert.Equal(groupName, member.PartitionKey);
        Assert.Equal(userId, member.RowKey);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(groupName, member.GroupName);
        Assert.Equal(language, member.Language);
        Assert.Equal(connectionId, member.ConnectionId);
        Assert.True(member.Timestamp.HasValue);
    }

    [Fact]
    public void ChatMember_ParameterizedConstructor_WithNullGroupName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ChatMember(null!, "user1", "en", "connection1"));
    }

    [Fact]
    public void ChatMember_ParameterizedConstructor_WithNullUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ChatMember("group1", null!, "en", "connection1"));
    }

    [Fact]
    public void ChatMember_ParameterizedConstructor_WithNullLanguage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ChatMember("group1", "user1", null!, "connection1"));
    }

    [Fact]
    public void ChatMember_ParameterizedConstructor_WithNullConnectionId_SetsEmptyString()
    {
        // Act && Assert
        Assert.Throws<ArgumentNullException>(() => new ChatMember("group1", "user1", "en", null));

    }

    #endregion

    #region Helper Methods

    private void VerifyNoTableOperations()
    {
        _mockTableClient.Verify(x => x.AddEntityAsync(It.IsAny<ChatMember>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        _mockTableClient.Verify(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<ETag>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockTableClient.Verify(x => x.GetEntityAsync<ChatMember>(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(It.IsAny<Expression<Func<ChatMember, bool>>>(), 
            It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockTableClient.Verify(x => x.QueryAsync<ChatMember>(It.IsAny<string>(), 
            It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AsyncPageable<ChatMember> CreateMockAsyncPageable(IEnumerable<ChatMember> members)
    {
        var mockAsyncPageable = new Mock<AsyncPageable<ChatMember>>();
        mockAsyncPageable.Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerator(members));
        return mockAsyncPageable.Object;
    }

    private static async IAsyncEnumerator<ChatMember> CreateAsyncEnumerator(IEnumerable<ChatMember> members)
    {
        foreach (var member in members)
        {
            yield return member;
        }
        await Task.CompletedTask;
    }

    #endregion
}
