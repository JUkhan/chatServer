using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace chatApp.Hubs
{
    public record UpcomingMessage(string Id, string GroupName, bool IsPrivate, string UsersJson, string Message, string? Origin);
    public class ChatHub : Hub
    {
       
        private readonly ILogger logger;
        private readonly IChatService chatService;
        public ChatHub(
            ILogger<ChatHub> logger,
            IChatService chatService
            ) {
            this.logger = logger;
            this.chatService = chatService;
        }
        public async Task ActiveUser(User currentUser, List<User> users)
        {
            if (currentUser == null || users == null)
            {
                return;
            }
            var origin = GetOrigin();
            if (origin == null) { return; }
            chatService.SetUsers(Context.ConnectionId, currentUser, users, origin);
            
            var groups = chatService.GetGroups(currentUser.Email, origin);
            foreach (var group in groups) {
                 await Groups.AddToGroupAsync(Context.ConnectionId, group.GroupName);
            }
            var status = chatService.GetUnreadStatuses(currentUser.Email, origin);
            await Clients.Client(Context.ConnectionId).SendAsync("activeUser", groups, status);
            await Clients.AllExcept(Context.ConnectionId).SendAsync("online", currentUser);
            logger.LogInformation($"{currentUser.Name} Connected");
        }
        private string? GetOrigin()
        {
            return Context.GetHttpContext()?.Request.Headers.Origin;
        }

        public async Task GroupMessage(User sender, UpcomingMessage um)
        {
            var message = chatService.CreateMessage(sender, um with { Origin=GetOrigin() });
            if (um.GroupName.EndsWith("All Users"))
            {
                await Clients.All.SendAsync("groupMessage", message, um.GroupName, um.IsPrivate);
            }
            else
            {
                await Clients.Group(um.GroupName).SendAsync("groupMessage", message, um.GroupName);
            }
        }
        public async Task NewGroup(User user, ChatGroup group)
        {
            var ng = chatService.CreateGroup(group with { Origin = GetOrigin() ?? string.Empty });
            if (ng == null)
            {
                await Clients.All.SendAsync("newGroup", group);
                return;
            }
            if(group.UsersJson.Contains(user.Email))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, group.GroupName);
            }
            await Clients.All.SendAsync("newGroup", ng, user.Email);
        }
        public async Task MessagesByGroupId(ChatGroup group)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("messagesByGroupId", chatService.chatMessages(group with { Origin = GetOrigin() ?? string.Empty }), group.GroupName);
        }
        public void Reconnected(User user)
        {
            chatService.Reconnected(Context.ConnectionId, user);
            logger.Log(LogLevel.Information, $"{user.Name} Reconnected");
        }
        public async Task SendMessage(string user, string message)
        {
            Debug.WriteLine($"Origin: {Context.GetHttpContext()?.Request.Headers["Origin"]}");
            logger.Log(LogLevel.Information, this.Context.ConnectionId);
            await Clients.All.SendAsync("ReceiveMessage", user, message);
            
        }
        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            await Clients.Group(roomName).SendAsync(Context.ConnectionId + " joined.");
        }

        public Task LeaveRoom(string roomName)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }
        public override Task OnConnectedAsync()
        {
            if (!chatService.IsRegistered(GetOrigin()??string.Empty))
            {
                Context.Abort();
            }
            logger.LogInformation($"{this.Context.ConnectionId} Connected");
            Clients.Client(Context.ConnectionId).SendAsync("connected");
            return base.OnConnectedAsync();
        }

        public override  Task OnDisconnectedAsync(Exception? exception)
        {
            var user=chatService.GetUser(Context.ConnectionId);
            logger.Log(LogLevel.Information, $"{user.Name} Disconnected");
            chatService.RemoveUser(Context.ConnectionId);
            Clients.All.SendAsync("offline", user);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
