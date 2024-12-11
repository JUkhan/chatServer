using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using chatApp.Models;

namespace chatApp.Hubs
{
    
    public class ChatHub : Hub
    {
       
        private readonly ILogger<ChatHub> logger;
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
            
            var groups = await chatService.GetGroups(currentUser.Email, origin);
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
            var message = await chatService.CreateMessage(sender, um with { Origin=GetOrigin() });
            logger.LogInformation($"{message}");
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
            var ng = await chatService.CreateGroup(group with { Origin = GetOrigin() ?? string.Empty });
            
            if(group.UsersJson.Contains(user.Email))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, group.GroupName);
            }
            await Clients.All.SendAsync("newGroup", ng, user.Email);
        }
        public async Task MessagesByGroupId(ChatGroup group)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("messagesByGroupId", await chatService.chatMessages(group with { Origin = GetOrigin() ?? string.Empty }), group.GroupName);
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
        public async override Task OnConnectedAsync()
        {
            if (!await chatService.IsRegistered(GetOrigin()??string.Empty))
            {
                Context.Abort();
            }
            logger.LogInformation($"{this.Context.ConnectionId} Connected");
            await Clients.Client(Context.ConnectionId).SendAsync("connected");
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
