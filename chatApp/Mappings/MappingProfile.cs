using AutoMapper;
using chatApp.DB;
using chatApp.Models;

namespace chatApp.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<ChatGroup, GroupEntity>().ReverseMap();
            CreateMap<ChatMessage, MessageEntity>().ReverseMap();
            CreateMap<ChatUnreadStatus, UnreadStatusEntity>().ReverseMap();
        }
    }
}
