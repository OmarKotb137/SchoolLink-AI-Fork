using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Conversations;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;

namespace Project.BLL.Services;

public class ConversationService : IConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ConversationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<ConversationDto>> CreateDirectConversationAsync(CreateDirectConversationRequest request)
    {
        var initiator = await _unitOfWork.Users.GetByIdAsync(request.InitiatorUserId);
        if (initiator == null || initiator.IsDeleted || !initiator.IsActive)
            return OperationResult<ConversationDto>.Failure("Initiator user not found or inactive");

        var target = await _unitOfWork.Users.GetByIdAsync(request.TargetUserId);
        if (target == null || target.IsDeleted || !target.IsActive)
            return OperationResult<ConversationDto>.Failure("Target user not found or inactive");

        var existing = await _unitOfWork.Conversations.GetDirectConversationAsync(
            request.InitiatorUserId, request.TargetUserId);

        if (existing != null)
        {
            var existingDto = await MapConversationWithParticipantsAsync(existing);
            return OperationResult<ConversationDto>.Success(existingDto, "Existing conversation returned");
        }

        var conversation = new Conversation
        {
            Type = ConversationType.Direct,
            LastMessageAt = DateTime.UtcNow
        };

        await _unitOfWork.Conversations.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        var participants = new[]
        {
            new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = request.InitiatorUserId,
                JoinedAt = DateTime.UtcNow
            },
            new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = request.TargetUserId,
                JoinedAt = DateTime.UtcNow
            }
        };

        await _unitOfWork.ConversationParticipants.AddRangeAsync(participants);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapConversationWithParticipantsAsync(conversation);
        return OperationResult<ConversationDto>.Success(dto, "Conversation created successfully");
    }

    public async Task<OperationResult<ConversationDto>> CreateGroupConversationAsync(CreateGroupConversationRequest request)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(request.CreatorUserId);
        if (creator == null || creator.IsDeleted || !creator.IsActive)
            return OperationResult<ConversationDto>.Failure("Creator not found or inactive");

        if (request.ParticipantUserIds.Count < 2)
            return OperationResult<ConversationDto>.Failure("At least 2 participants are required");

        var allUserIds = new List<int> { request.CreatorUserId };
        allUserIds.AddRange(request.ParticipantUserIds);

        foreach (var userId in allUserIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.IsDeleted || !user.IsActive)
                return OperationResult<ConversationDto>.Failure($"User with id {userId} not found or inactive");
        }

        var conversation = new Conversation
        {
            Title = request.Title,
            Type = ConversationType.Group,
            LastMessageAt = DateTime.UtcNow
        };

        await _unitOfWork.Conversations.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        var participants = allUserIds.Select(userId => new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        }).ToList();

        await _unitOfWork.ConversationParticipants.AddRangeAsync(participants);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapConversationWithParticipantsAsync(conversation);
        return OperationResult<ConversationDto>.Success(dto, "Group conversation created successfully");
    }

    public async Task<OperationResult<MessageDto>> SendMessageAsync(SendMessageRequest request)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult<MessageDto>.Failure("Conversation not found");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(
            request.ConversationId, request.SenderId);

        if (!isParticipant)
            return OperationResult<MessageDto>.Failure("Sender is not a participant in this conversation");

        if (string.IsNullOrWhiteSpace(request.Content))
            return OperationResult<MessageDto>.Failure("Message content cannot be empty");

        var message = _mapper.Map<Message>(request);
        message.SentAt = DateTime.UtcNow;

        await _unitOfWork.Messages.AddAsync(message);

        conversation.LastMessageAt = DateTime.UtcNow;
        _unitOfWork.Conversations.Update(conversation);

        await _unitOfWork.SaveChangesAsync();

        var sender = await _unitOfWork.Users.GetByIdAsync(request.SenderId);
        var dto = _mapper.Map<MessageDto>(message);
        dto.SenderName = sender?.FullName ?? "";

        return OperationResult<MessageDto>.Success(dto, "Message sent successfully");
    }

    public async Task<OperationResult<PagedResult<MessageDto>>> GetMessagesAsync(
        int conversationId, int requestingUserId, PaginationFilter filter)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult<PagedResult<MessageDto>>.Failure("Conversation not found");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(
            conversationId, requestingUserId);

        if (!isParticipant)
            return OperationResult<PagedResult<MessageDto>>.Failure("User is not a participant in this conversation");

        var messages = await _unitOfWork.Messages.GetByConversationPagedAsync(
            conversationId, filter.Page, filter.PageSize);

        await _unitOfWork.ConversationParticipants.UpdateLastReadAtAsync(
            conversationId, requestingUserId, DateTime.UtcNow);

        var totalCount = await _unitOfWork.Messages.CountAsync(m => m.ConversationId == conversationId);

        var dtos = _mapper.Map<IEnumerable<MessageDto>>(messages);

        var paged = new PagedResult<MessageDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return OperationResult<PagedResult<MessageDto>>.Success(paged);
    }

    private async Task<ConversationDto> MapConversationWithParticipantsAsync(Conversation conversation)
    {
        var dto = _mapper.Map<ConversationDto>(conversation);

        var participants = await _unitOfWork.ConversationParticipants
            .GetByConversationIdAsync(conversation.Id);

        var participantDtos = new List<ConversationParticipantDto>();
        foreach (var participant in participants)
        {
            var pDto = _mapper.Map<ConversationParticipantDto>(participant);
            var user = await _unitOfWork.Users.GetByIdAsync(participant.UserId);
            pDto.UserName = user?.FullName ?? "";
            participantDtos.Add(pDto);
        }

        dto.Participants = participantDtos;
        return dto;
    }
}