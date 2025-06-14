using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalTranslator;


public class ChatMember 
    : ITableEntity
{
    public ChatMember(string groupName, string userId, string language)
    {
        PartitionKey = groupName ?? throw new ArgumentNullException(nameof(groupName));
        RowKey = userId ?? throw new ArgumentNullException(nameof(userId));
        GroupName = groupName;
        UserId = userId;
        Language = language ?? throw new ArgumentNullException(nameof(language));
        ConnectionId = string.Empty;
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
        if (member == null) throw new ArgumentNullException(nameof(member));
        if (string.IsNullOrWhiteSpace(member.PartitionKey)) throw new ArgumentNullException(nameof(member.PartitionKey));
        if (string.IsNullOrWhiteSpace(member.RowKey)) throw new ArgumentNullException(nameof(member.RowKey));
        await _tableClient.AddEntityAsync(member);
    }

    public async Task RemoveChatMemberAsync(string partitionKey, string rowKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
        if (string.IsNullOrWhiteSpace(rowKey)) throw new ArgumentNullException(nameof(rowKey));
        
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task<ChatMember?> GetChatMemberAsync(string partitionKey, string rowKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
        if (string.IsNullOrWhiteSpace(rowKey)) throw new ArgumentNullException(nameof(rowKey));
        
        return await _tableClient.GetEntityAsync<ChatMember>(partitionKey, rowKey);
    }

    public async Task<IDictionary<string, ChatMember>> GetChatMembersAsync(string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
        
        var query = _tableClient.QueryAsync<ChatMember>(member => member.PartitionKey == partitionKey);
        var results = new List<ChatMember>();
        await foreach (var member in query)
        {
            results.Add(member);
        }
        return results.ToDictionary(m => m.RowKey, m => m);
    }

}
