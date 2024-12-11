using System.Net.NetworkInformation;
using chatApp.Models;

namespace chatApp.Hubs
{
    public interface IChatService
    {
        void SetUsers(string connectionId, User currentUser, List<User> users, string origin);
        Task<List<ChatGroup>> GetGroups(string userId, string Origin);
        User GetUser(string connectionId);
        void RemoveUser(string connectionId);
        Task<ChatMessage> CreateMessage(User sender, UpcomingMessage um);
        Task<ChatGroup> CreateGroup(ChatGroup group);
        Task<Dictionary<string, int>> GetUnreadStatuses(string userId, string origin);
        Task<List<ChatMessage>> chatMessages(ChatGroup group);
        void Reconnected(string connectionId, User user);
        Task<bool> IsRegistered(string origin);
    }
}

