using System.Diagnostics;
using System.Text.Json;

namespace chatApp.Hubs
{
    public record Register(string Origin, string CompanyName);
    public record User(string Name, string Email);
    public record ChatGroup(string Id, string Origin, string GroupName,bool IsPrivate, string UsersJson);
    //public record ChatGroupView(string Id, string GroupName, bool IsPrivate, List<User> users);
    public record ChatMessage(string GroupId, string Message, string UserName, DateTime CreatedAt);
    public record ChatUnreadStatus(string UserId, string GroupName, string Origin);
    public class ChatService : IChatService
    {
        private IDictionary<string, User> activeUsers = new Dictionary<string, User>();
        private Dictionary<string,List<User>> users = new Dictionary<string, List<User>>();
        private List<ChatGroup> groups = new List<ChatGroup>();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private List<ChatUnreadStatus> unreadStatus = new List<ChatUnreadStatus>();
        private List<Register> origins = new List<Register>();
        public ChatService() {
            origins.Add(new Register(Origin: "http://localhost:3000", CompanyName: "com1"));
            origins.Add(new Register(Origin: "http://localhost:3001", CompanyName: "com2"));
            foreach (var item in origins)
            {
                CreateGroup(new ChatGroup(Guid.NewGuid().ToString(), item.Origin, "All Users", false, UsersJson: ""));
            }
            
        }
        public List<ChatGroup> GetGroups(string userId, string origin)
        { 
            return this.groups.Where(group=> group.Origin == origin)
                .Where(group => group.UsersJson.Contains(userId)||group.GroupName.EndsWith("All Users"))
                .ToList();
        }

        public bool IsRegistered(string origin) {
            return origins.Any(it => it.Origin == origin);
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

        public void SetUsers(string connectionId, User currentUser, List<User> users, string origin)
        {
            if (!activeUsers.ContainsKey(connectionId))
            {
                activeUsers.Add(connectionId, currentUser);
            }
            this.users[origin] = users;
        }

        public ChatMessage CreateMessage(User sender, UpcomingMessage um)
        {
            var message = new ChatMessage(GroupId: um.Id, Message: um.Message, UserName: sender.Name, CreatedAt: DateTime.Now);
            messages.Add(message);
            SendEmail(sender, um);
            Debug.WriteLine(message);
            return message;
        }

        public string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public ChatGroup? CreateGroup(ChatGroup group)
        {
            string groupName = $"$@&${Base64Encode(group.Origin)}$@&${group.GroupName}";
            if (groups.Where(it=>it.Origin==group.Origin).Any(g => g.GroupName.Equals(groupName)))
            {
                return null;
            }
            var newGroup= group with {Id= Guid.NewGuid().ToString(), GroupName=groupName };
            groups.Add(newGroup);
            return newGroup;
        }
        private void SendEmail(User sender, UpcomingMessage um)
        {
            var onlineUsers=activeUsers.Values.ToDictionary(it => it.Email);
            var offlineUsers = new List<User>();
            foreach(var user in (um.GroupName.EndsWith("All Users")? users[um.Origin] : JsonSerializer.Deserialize<List<User>>(um.UsersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })) ?? []) {
                if (!onlineUsers.ContainsKey(user.Email))
                {
                    offlineUsers.Add(user);
                }
            }
            foreach (var item in offlineUsers)
            {
                CreateUnreadStatus(item.Email, um.GroupName, um.Origin??string.Empty);
            }
            Debug.WriteLine("Email send to Offline users:");
            Debug.WriteLine(offlineUsers);
        }

        public ChatUnreadStatus CreateUnreadStatus(string userId, string groupName, string origin)
        {
            var status= new ChatUnreadStatus(userId, groupName, origin);
            unreadStatus.Add(status);
            return status;
        }

        public void RemoveUnreadStatusByGroupName(string userId, string groupName)
        {
            this.unreadStatus=this.unreadStatus.Where(it=>!(it.UserId==userId && it.GroupName==groupName)).ToList();
        }

        public Dictionary<string, int> GetUnreadStatuses(string userId, string origin)
        {
            var data= this.unreadStatus.Where(it=>it.Origin==origin).Where(it=>it.UserId==userId).ToList();
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
            //remove unread status
            unreadStatus = unreadStatus.Where(it=>it.Origin==group.Origin).Where(it=>it.GroupName!=group.GroupName).ToList();
            return messages.Where(it=>it.GroupId==group.Id).ToList();
        }

        public void Reconnected(string connectionId, User user)
        {
            activeUsers.Add(connectionId, user);
        }
    }
}
