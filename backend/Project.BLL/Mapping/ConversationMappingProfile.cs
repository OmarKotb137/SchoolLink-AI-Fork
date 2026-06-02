using AutoMapper;
using Project.BLL.DTOs.Conversations;
using Project.Domain.Entities;

namespace Project.BLL.Mapping;

public class ConversationMappingProfile : Profile
{
    public ConversationMappingProfile()
    {
        CreateMap<Conversation, ConversationDto>();

        CreateMap<ConversationParticipant, ConversationParticipantDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FullName));

        CreateMap<Message, MessageDto>()
            .ForMember(d => d.SenderName, o => o.MapFrom(s => s.Sender.FullName));

        CreateMap<SendMessageRequest, Message>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.SentAt, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Conversation, o => o.Ignore())
            .ForMember(d => d.Sender, o => o.Ignore());
    }
}
