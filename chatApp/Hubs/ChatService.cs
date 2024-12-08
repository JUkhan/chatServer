using System.Diagnostics;
using System.Text.Json;

namespace chatApp.Hubs
{
    public record MUser(string name, string email);
    public record User(string Name, string Email);
    public record ChatGroup(string Id, string GroupName,bool IsPrivate, string UsersJson);
    public record ChatGroupView(string Id, string GroupName, bool IsPrivate, List<User> users);
    public record ChatMessage(string GroupId, string Message, string UserName, DateTime CreatedAt);
    public record ChatUnreadStatus(string UserId, string GroupName);
    public class ChatService : IChatService
    {
        private IDictionary<string, User> activeUsers = new Dictionary<string, User>();
        private List<User> users = new List<User>();
        private List<ChatGroup> groups = new List<ChatGroup>();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private List<ChatUnreadStatus> unreadStatus = new List<ChatUnreadStatus>();
        public ChatService() {
            CreateGroup(new ChatGroup(Guid.NewGuid().ToString(), "All Users", false, UsersJson:""));
        }
        public List<ChatGroup> GetGroups(string userId)
        {
            return this.groups.Where(group => group.UsersJson.Contains(userId)||group.GroupName=="All Users").ToList();
        }

        public User GetUser(string connectionId)
        {
            if(activeUsers.ContainsKey(connectionId)) {  return activeUsers[connectionId]; }
            return new User("", "");
        }

        public void RemoveUser(string connectionId)
        {
            activeUsers.Remove(connectionId);
        }

        public void SetUsers(string connectionId, User currentUser, List<User> users)
        {
            if (!activeUsers.ContainsKey(connectionId))
            {
                activeUsers.Add(connectionId, currentUser);
            }
            this.users = users;
        }

        public ChatMessage CreateMessage(User sender, UpcomingMessage um)
        {
            var message = new ChatMessage(GroupId: um.Id, Message: um.Message, UserName: sender.Name, CreatedAt: DateTime.Now);
            messages.Add(message);
            SendEmail(sender, um);
            Debug.WriteLine(message);
            return message;
        }

        public ChatGroup? CreateGroup(ChatGroup group)
        {
            if (groups.Any(g => g.GroupName.Equals(group.GroupName)))
            {
                return null;
            }
            var newGroup= group with {Id= Guid.NewGuid().ToString() };
            groups.Add(newGroup);
            return newGroup;
        }
        private void SendEmail(User sender, UpcomingMessage um)
        {
            var onlineUsers=activeUsers.Values.ToDictionary(it => it.Email);
            var offlineUsers = new List<User>();
            foreach(var user in (um.GroupName == "All Users"? users: JsonSerializer.Deserialize<List<User>>(um.UsersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })) ?? []) {
                if (!onlineUsers.ContainsKey(user.Email))
                {
                    offlineUsers.Add(user);
                }
            }
            foreach (var item in offlineUsers)
            {
                CreateUnreadStatus(item.Email, um.GroupName);
            }
            Debug.WriteLine("Email send to Offline users:");
            Debug.WriteLine(offlineUsers);
        }

        public ChatUnreadStatus CreateUnreadStatus(string userId, string groupName)
        {
            var status= new ChatUnreadStatus(userId, groupName);
            unreadStatus.Add(status);
            return status;
        }

        public void RemoveUnreadStatusByGroupName(string userId, string groupName)
        {
            this.unreadStatus=this.unreadStatus.Where(it=>!(it.UserId==userId && it.GroupName==groupName)).ToList();
        }

        public Dictionary<string, int> GetUnreadStatuses(string userId)
        {
            var data= this.unreadStatus.Where(it=>it.UserId==userId).ToList();
            var dic=new Dictionary<string, int>();
            foreach (var item in data)
            {
                if (dic.ContainsKey(item.GroupName))
                {
                    dic[item.GroupName]++;
                }
                else
                {
                    dic[item.GroupName] = 1;
                }
            }
            return dic;
        }

        public List<ChatMessage> chatMessages(ChatGroup group)
        {
            unreadStatus = unreadStatus.Where(it=>it.GroupName!=group.GroupName).ToList();
            return messages.Where(it=>it.GroupId==group.Id).ToList();
        }

        public void Reconnected(string connectionId, User user)
        {
            activeUsers.Add(connectionId, user);
        }
    }
}
