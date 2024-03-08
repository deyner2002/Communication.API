using APICommunication.DTOs;
using APIEmisorKafka.Models;
using AutoMapper;

namespace APICommunication.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<NotificationRequest, Notification>()
                .ForMember(noti => noti.Contacts, opt => opt.MapFrom(src => src.ContactInfo.Contacts))
                .ForMember(noti => noti.Templates, opt => opt.MapFrom(src => new List<Template>()));
        }
    }
}
