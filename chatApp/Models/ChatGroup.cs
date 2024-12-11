namespace chatApp.Models
{
    public record ChatGroup(string Id, string Origin, string GroupName, bool IsPrivate, string UsersJson);
}
