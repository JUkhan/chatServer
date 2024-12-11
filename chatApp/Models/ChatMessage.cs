namespace chatApp.Models
{
    public record ChatMessage(string GroupId, string Message, string UserName, DateTime CreatedAt);
}
