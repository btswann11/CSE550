using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalTranslator;


public class ChatMember 
    : ITableEntity
{
    public ChatMember()
    {
        PartitionKey = string.Empty;
        RowKey = string.Empty;
        UserId = string.Empty;
        GroupName = string.Empty;
        Language = string.Empty;
        ConnectionId = string.Empty;
    }
    public ChatMember(string groupName, string userId, string language, string? connectionId)
    {
        PartitionKey = groupName ?? throw new ArgumentNullException(nameof(groupName));
        RowKey = userId ?? throw new ArgumentNullException(nameof(userId));
        GroupName = groupName;
        UserId = userId;
        Language = language ?? throw new ArgumentNullException(nameof(language));
        ConnectionId = connectionId ?? string.Empty;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string UserId { get; set; }
    public string GroupName { get; set; }
    public string Language { get; set; }
    public string ConnectionId { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class StorageService
{
    private readonly TableClient _tableClient;

    public StorageService(TableClient tableClient)
    {
        _tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        _tableClient.CreateIfNotExists();
    }

    public async Task AddChatMemberAsync(ChatMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(member.PartitionKey, nameof(member.PartitionKey));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(member.RowKey, nameof(member.RowKey));
        await _tableClient.AddEntityAsync(member);
    }

    public async Task RemoveChatMemberAsync(string partitionKey, string rowKey)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(partitionKey, nameof(partitionKey));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(rowKey, nameof(rowKey));

        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task<ChatMember?> GetChatMemberAsync(string partitionKey, string rowKey)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(partitionKey, nameof(partitionKey));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(rowKey, nameof(rowKey));

        return await _tableClient.GetEntityAsync<ChatMember>(partitionKey, rowKey);
    }

    public async Task<IDictionary<string, ChatMember>> GetChatMembersAsync(string partitionKey)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(partitionKey, nameof(partitionKey));

        var query = _tableClient.QueryAsync<ChatMember>(member => member.PartitionKey == partitionKey);
        var results = new List<ChatMember>();
        await foreach (var member in query)
        {
            results.Add(member);
        }
        return results.ToDictionary(m => m.RowKey, m => m);
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        var query = _tableClient.QueryAsync<ChatMember>(member => member.RowKey == userId);
        var results = new List<ChatMember>();
        await foreach (var member in query)
        {
            results.Add(member);
        }

        return results.Count > 0;
    }

    public async Task DeleteUserAsync(string userId)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        var query = _tableClient.QueryAsync<ChatMember>(member => member.RowKey == userId);
        await foreach (var member in query)
        {
            await _tableClient.DeleteEntityAsync(member.PartitionKey, member.RowKey);
        }
    }

    public async Task<IEnumerable<ChatMember>> GetOnlineUsersAsync()
    {
        var query = _tableClient.QueryAsync<ChatMember>();
        var results = new List<ChatMember>();
        await foreach (var member in query)
        {
            results.Add(member);
        }
        return results;
    }

    private static async Task<List<ChatMember>> ToUsersAsync(AsyncPageable<ChatMember> query)
    {
        var results = new List<ChatMember>();
        await foreach (var member in query)
        {
            results.Add(member);
        }

        return results;
    }
}
