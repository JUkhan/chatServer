using System.Net.NetworkInformation;

namespace chatApp.Hubs
{
    public interface IChatService
    {
        void SetUsers(string connectionId, User currentUser, List<User> users, string origin);
        List<ChatGroup> GetGroups(string userId, string Origin);
        User GetUser(string connectionId);
        void RemoveUser(string connectionId);
        ChatMessage CreateMessage(User sender, UpcomingMessage um);
        ChatGroup? CreateGroup(ChatGroup group);
        //ChatUnreadStatus CreateUnreadStatus(string userId, string groupName);
        void RemoveUnreadStatusByGroupName(string userId, string groupName);
        Dictionary<string, int> GetUnreadStatuses(string userId, string origin);
        List<ChatMessage> chatMessages(ChatGroup group);
        void Reconnected(string connectionId, User user);
        bool IsRegistered(string origin);
        string Base64Encode(string plainText);
    }
}

