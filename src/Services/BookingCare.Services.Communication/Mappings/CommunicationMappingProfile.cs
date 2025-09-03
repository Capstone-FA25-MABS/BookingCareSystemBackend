using AutoMapper;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Mappings;

/// <summary>
/// AutoMapper profile cho Communication models
/// </summary>
public class CommunicationMappingProfile : Profile
{
    public CommunicationMappingProfile()
    {
        CreateMessageMappings();
        CreateConversationMappings();
        CreateCallLogMappings();
    }

    /// <summary>
    /// T?o mappings cho Message
    /// </summary>
    private void CreateMessageMappings()
    {
        // Message Entity to Response
        CreateMap<MessageEntity, MessageResponse>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

        // Create Request to Entity
        CreateMap<CreateMessageRequest, MessageEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enums.MessageStatus.UNREAD))
            .ForMember(dest => dest.ReadAt, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

        // Update Request to Entity
        CreateMap<UpdateMessageRequest, MessageEntity>()
            .ForMember(dest => dest.ConversationId, opt => opt.Ignore())
            .ForMember(dest => dest.SenderId, opt => opt.Ignore())
            .ForMember(dest => dest.ReceiverId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ReadAt, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

        // MessageAttachment mappings
        CreateMap<MessageAttachmentRequest, MessageAttachment>();
        CreateMap<MessageAttachment, MessageAttachmentResponse>();
    }

    /// <summary>
    /// T?o mappings cho Conversation
    /// </summary>
    private void CreateConversationMappings()
    {
        // Conversation Entity to Response
        CreateMap<ConversationEntity, ConversationResponse>()
            .ForMember(dest => dest.LastMessage, opt => opt.MapFrom(src => src.LastMessage))
            .ForMember(dest => dest.Blocked, opt => opt.MapFrom(src => src.Blocked));

        // Create Request to Entity
        CreateMap<CreateConversationRequest, ConversationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.LastMessage, opt => opt.Ignore())
            .ForMember(dest => dest.Blocked, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        // LastMessage mappings
        CreateMap<LastMessage, LastMessageResponse>();
        CreateMap<BlockedInfo, BlockedInfoResponse>();
    }

    /// <summary>
    /// T?o mappings cho CallLog
    /// </summary>
    private void CreateCallLogMappings()
    {
        // CallLog Entity to Response
        CreateMap<CallLogEntity, CallLogResponse>();

        // Create Request to Entity
        CreateMap<CreateCallLogRequest, CallLogEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
            .ForMember(dest => dest.EndedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enums.CallStatus.Missed));

        // Update Request to Entity (ch? update m?t s? fields)
        CreateMap<UpdateCallLogRequest, CallLogEntity>()
            .ForMember(dest => dest.ConversationId, opt => opt.Ignore())
            .ForMember(dest => dest.CallerId, opt => opt.Ignore())
            .ForMember(dest => dest.ReceiverId, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore());

        // CallStatistics mappings
        CreateMap<Repositories.Interfaces.CallStatistics, CallStatisticsResponse>();
    }
}