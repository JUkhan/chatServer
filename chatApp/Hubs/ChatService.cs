using chatApp.DB;
using System.Text.Json;
using chatApp.Models;
using AutoMapper;

namespace chatApp.Hubs
{
    public class ChatService : IChatService
    {
        private static IDictionary<string, User> activeUsers = new Dictionary<string, User>();
        private static Dictionary<string, List<User>> users = new Dictionary<string, List<User>>();
        private readonly ILogger<ChatService> _logger;
        private readonly IRegisterRepository _registerRepository;
        private readonly IMapper _mapper;
        private readonly IGroupRepository _groupRepository;
        private readonly IUnreadStatusRepository _unreadStatusRepository;
        private readonly IMessageRepository _messageRepository;
        public ChatService(
            IMessageRepository messageRepository,
            IUnreadStatusRepository unreadStatusRepository,
            IGroupRepository groupRepository,
            ILogger<ChatService> logger,
            IMapper mapper,
            IRegisterRepository registerRepository)
        {
            _mapper = mapper;
            _registerRepository = registerRepository ?? throw new ArgumentNullException(nameof(registerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _groupRepository = groupRepository;
            _unreadStatusRepository = unreadStatusRepository;
            _messageRepository = messageRepository;
        }
        public async Task<List<ChatGroup>> GetGroups(string userId, string origin)
        {
            var groups = await _groupRepository.GetAsync(it => it.Origin == origin && (it.UsersJson.Contains(userId) || it.GroupName.EndsWith("All Users")));
            return _mapper.Map<List<ChatGroup>>(groups);
        }

        public async Task<bool> IsRegistered(string origin)
        {
            var dd = await _registerRepository.GetAsync(it => it.Origin == origin);
            return dd.Any();
        }
        public User GetUser(string connectionId)
        {
            if (activeUsers.ContainsKey(connectionId)) { return activeUsers[connectionId]; }
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
                activeUsers.Add(connectionId, currentUser with { Origin = origin });
            }
            ChatService.users[origin] = users;
        }

        public async Task<ChatMessage> CreateMessage(User sender, UpcomingMessage um)
        {
            var message = new MessageEntity { Id = Guid.NewGuid().ToString(), GroupId = um.Id, Message = um.Message, UserName = sender.Name, CreatedAt = DateTime.Now };
            message = await _messageRepository.AddAsync(message);
            await SendEmail(sender, um);
            return _mapper.Map<ChatMessage>(message);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public async Task<ChatGroup> CreateGroup(ChatGroup group)
        {
            string groupName = $"$@&${Base64Encode(group.Origin)}$@&${group.GroupName}";
            group = group with { GroupName = groupName };
            var groups = await _groupRepository.GetAsync(it => it.Origin == group.Origin && it.GroupName == groupName);
            if (groups.Any())
            {
                return group;
            }
            var newGroup = _mapper.Map<GroupEntity>(group);
            newGroup.Id = Guid.NewGuid().ToString();
            newGroup = await _groupRepository.AddAsync(newGroup);
            return _mapper.Map<ChatGroup>(newGroup);
        }
        private async Task<bool> SendEmail(User sender, UpcomingMessage um)
        {
            var onlineUsers = activeUsers.Values.Where(it => it != null && it.Origin == um.Origin)
                .DistinctBy(it => it.Email)
                .ToDictionary(it => it.Email);
            var offlineUsers = new List<User>();
            foreach (var user in (um.GroupName.EndsWith("All Users") ? users[um.Origin!] : JsonSerializer.Deserialize<List<User>>(um.UsersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })) ?? [])
            {
                if (!onlineUsers.ContainsKey(user.Email))
                {
                    offlineUsers.Add(user);
                }
            }

            if (offlineUsers.Any())
            {
                await _unreadStatusRepository.AddRangeAsync(offlineUsers.Select(it => new UnreadStatusEntity { Id = Guid.NewGuid().ToString(), UserId = it.Email, GroupName = um.GroupName, Origin = um.Origin! }));
            }
            _logger.LogInformation("Email send to Offline users:");
            _logger.LogInformation(string.Join(", ", offlineUsers.Select(it => it.Email)));
            return true;
        }

        public async Task<Dictionary<string, int>> GetUnreadStatuses(string userId, string origin)
        {
            var data = await _unreadStatusRepository.GetAsync(it => it.Origin == origin && it.UserId == userId);
            var dic = new Dictionary<string, int>();
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

        public async Task<List<ChatMessage>> chatMessages(ChatGroup group)
        {
            var data = await _unreadStatusRepository.GetAsync(it => it.Origin == group.Origin && it.GroupName == group.GroupName);
            if (data.Any())
            {
                await _unreadStatusRepository.DeleteRangeAsync(data);
            }
            var messages = await _messageRepository.GetAsync(it => it.GroupId == group.Id);

            return _mapper.Map<List<ChatMessage>>(messages);
        }

        public void Reconnected(string connectionId, User user)
        {
            activeUsers.Add(connectionId, user);
        }
    }
}
