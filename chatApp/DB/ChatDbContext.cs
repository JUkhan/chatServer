using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace chatApp.DB
{
    public class ChatDbContext(DbContextOptions<ChatDbContext> options): DbContext(options)
    {
        /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("myDB");
            base.OnConfiguring(optionsBuilder);
        }*/
        public DbSet<RegisterEntity> RegisterEntities { get; set; }
        public DbSet<GroupEntity> GroupEntities { get; set; }
        public DbSet<MessageEntity> MessageEntities { get; set; }
        public DbSet<UnreadStatusEntity> UnreadStatuses { get; set; }

    }

    public class EntityBase { }
    public class RegisterEntity: EntityBase
    {
        [Key]
        public string Origin { get; set; }
        public string CompanyName { get; set;}
    }

    public class GroupEntity : EntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Origin { get; set; }
        public string GroupName { get; set; }
        public bool IsPrivate { get; set; }
        public string UsersJson { get; set; }
    }

    public class MessageEntity : EntityBase
    {
        public string Id { get; set; }= Guid.NewGuid().ToString();
        public string GroupId { get; set; }
        public string Message { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }

    }
    
    public class UnreadStatusEntity : EntityBase
    {
        public string Id { get; set; }= Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string GroupName { get; set; }
        public string Origin { get; set; }
    }
}
