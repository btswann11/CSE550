
namespace UniversalTranslator;

public interface IStorageService
{
    Task AddChatMemberAsync(ChatMember member);
    Task DeleteUserAsync(string userId);
    Task<ChatMember?> GetChatMemberAsync(string partitionKey, string rowKey);
    Task<IDictionary<string, ChatMember>> GetChatMembersAsync(string partitionKey);
    Task<IEnumerable<ChatMember>> GetOnlineUsersAsync();
    Task<bool> IsUserOnlineAsync(string userId);
    Task RemoveChatMemberAsync(string partitionKey, string rowKey);
}