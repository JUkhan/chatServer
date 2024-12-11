namespace chatApp.Models
{
    public record UpcomingMessage(string Id, string GroupName, bool IsPrivate, string UsersJson, string Message, string? Origin);
}
