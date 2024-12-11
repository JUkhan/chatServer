using chatApp.DB;
using chatApp.Hubs;
using chatApp.Models;

namespace chatApp.Seed
{
    public class AppDbInitializer
    {
        public static void Seed(IApplicationBuilder builder)
        {
            using(var serviceScope = builder.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<ChatDbContext>();
                
                context.Database.EnsureCreated();

                if(!context.Registers.Any())
                {
                    var list=new List<RegisterEntity>();
                    list.Add(new RegisterEntity { Origin= "http://localhost:3000", CompanyName="com1"});
                    list.Add(new RegisterEntity { Origin = "http://localhost:3001", CompanyName = "com2" });
                    context.Registers.AddRange(list);
                    foreach (var register in list)
                    {
                        string groupName = $"$@&${ChatService.Base64Encode(register.Origin)}$@&$All Users";
                        context.Groups.Add(new GroupEntity {Id=Guid.NewGuid().ToString(), Origin=register.Origin, GroupName=groupName, IsPrivate=false,UsersJson="" });
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
