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

        if (creator.Role == UserRole.Student || creator.Role == UserRole.Parent)
            return OperationResult<ConversationDto>.Failure("Only Admins and Teachers can create group conversations");

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

    public async Task<OperationResult<ConversationDto>> CreateSubjectGroupConversationAsync(CreateSubjectGroupConversationRequest request)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(request.CreatorUserId);
        if (creator == null || creator.IsDeleted || !creator.IsActive)
            return OperationResult<ConversationDto>.Failure("Creator not found or inactive");

        if (creator.Role != UserRole.Admin && creator.Role != UserRole.Teacher)
            return OperationResult<ConversationDto>.Failure("Only Admins and Teachers can create subject group conversations");

        var subject = await _unitOfWork.Subjects.GetByIdAsync(request.SubjectId);
        if (subject == null || subject.IsDeleted)
            return OperationResult<ConversationDto>.Failure("Subject not found");

        var schoolClass = await _unitOfWork.Classes.GetByIdAsync(request.ClassId);
        if (schoolClass == null || schoolClass.IsDeleted)
            return OperationResult<ConversationDto>.Failure("Class not found");

        var cst = await _unitOfWork.ClassSubjectTeachers.GetByClassSubjectAndYearAsync(
            request.ClassId, request.SubjectId, request.AcademicYearId);

        if (cst == null)
            return OperationResult<ConversationDto>.Failure("No teacher assigned to this subject in this class");

        var enrollments = await _unitOfWork.StudentEnrollments.GetActiveByClassAsync(
            request.ClassId, request.AcademicYearId);

        var studentUserIds = enrollments
            .Select(e => e.Student.UserId)
            .Where(id => id.HasValue)
            .Cast<int>()
            .ToList();

        var allUserIds = new List<int> { cst.TeacherId };
        allUserIds.AddRange(studentUserIds);

        if (!allUserIds.Contains(request.CreatorUserId))
            allUserIds.Add(request.CreatorUserId);

        allUserIds = allUserIds.Distinct().ToList();

        var title = request.Title ?? $"{subject.Name} - {schoolClass.Name}";

        var conversation = new Conversation
        {
            Title = title,
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
        return OperationResult<ConversationDto>.Success(dto, "Subject group conversation created successfully");
    }

    public async Task<OperationResult> DeleteConversationAsync(int conversationId, int userId)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult.Failure("Conversation not found");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, userId);
        if (!isParticipant)
            return OperationResult.Failure("User is not a participant in this conversation");

        _unitOfWork.Conversations.SoftDelete(conversation);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("Conversation deleted successfully");
    }

    public async Task<OperationResult<ConversationDto>> UpdateConversationTitleAsync(int conversationId, string title, int userId)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult<ConversationDto>.Failure("Conversation not found");

        if (conversation.Type != ConversationType.Group)
            return OperationResult<ConversationDto>.Failure("Can only update title of group conversations");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, userId);
        if (!isParticipant)
            return OperationResult<ConversationDto>.Failure("User is not a participant in this conversation");

        if (string.IsNullOrWhiteSpace(title))
            return OperationResult<ConversationDto>.Failure("Title cannot be empty");

        conversation.Title = title;
        conversation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Conversations.Update(conversation);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapConversationWithParticipantsAsync(conversation);
        return OperationResult<ConversationDto>.Success(dto, "Title updated successfully");
    }

    public async Task<OperationResult<IEnumerable<ConversationParticipantDto>>> GetConversationParticipantsAsync(int conversationId, int userId)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult<IEnumerable<ConversationParticipantDto>>.Failure("Conversation not found");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, userId);
        if (!isParticipant)
            return OperationResult<IEnumerable<ConversationParticipantDto>>.Failure("User is not a participant in this conversation");

        var participants = await _unitOfWork.ConversationParticipants.GetByConversationIdAsync(conversationId);
        var dtos = new List<ConversationParticipantDto>();
        foreach (var p in participants)
        {
            var pDto = _mapper.Map<ConversationParticipantDto>(p);
            var user = await _unitOfWork.Users.GetByIdAsync(p.UserId);
            pDto.UserName = user?.FullName ?? "";
            dtos.Add(pDto);
        }

        return OperationResult<IEnumerable<ConversationParticipantDto>>.Success(dtos);
    }

    public async Task<OperationResult> MarkConversationAsReadAsync(int conversationId, int userId)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult.Failure("Conversation not found");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, userId);
        if (!isParticipant)
            return OperationResult.Failure("User is not a participant in this conversation");

        await _unitOfWork.ConversationParticipants.UpdateLastReadAtAsync(conversationId, userId, DateTime.UtcNow);
        return OperationResult.Success("Conversation marked as read");
    }

    public async Task<OperationResult<IEnumerable<ConversationDto>>> SearchConversationsAsync(int userId, string term)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult<IEnumerable<ConversationDto>>.Failure("User not found");

        var conversations = await _unitOfWork.Conversations.GetWithLastMessageAsync(userId);
        var filtered = conversations
            .Where(c => c.Title != null && c.Title.Contains(term, StringComparison.OrdinalIgnoreCase));

        var dtos = new List<ConversationDto>();
        foreach (var conversation in filtered)
        {
            var dto = await MapConversationWithParticipantsAsync(conversation);
            dtos.Add(dto);
        }

        return OperationResult<IEnumerable<ConversationDto>>.Success(dtos);
    }

    public async Task<OperationResult<ConversationDto>> GetConversationByIdAsync(int conversationId, int requestingUserId)
    {
        var conversation = await _unitOfWork.Conversations.GetWithParticipantsAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult<ConversationDto>.Failure("Conversation not found");

        var isParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, requestingUserId);
        if (!isParticipant)
            return OperationResult<ConversationDto>.Failure("User is not a participant in this conversation");

        var dto = await MapConversationWithParticipantsAsync(conversation);
        return OperationResult<ConversationDto>.Success(dto);
    }

    public async Task<OperationResult<IEnumerable<ConversationDto>>> GetUserConversationsAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return OperationResult<IEnumerable<ConversationDto>>.Failure("User not found");

        var conversations = await _unitOfWork.Conversations.GetWithLastMessageAsync(userId);

        var dtos = new List<ConversationDto>();
        foreach (var conversation in conversations)
        {
            var dto = await MapConversationWithParticipantsAsync(conversation);
            dtos.Add(dto);
        }

        return OperationResult<IEnumerable<ConversationDto>>.Success(dtos.OrderByDescending(c => c.LastMessageAt));
    }

    public async Task<OperationResult<int>> GetUnreadMessagesCountAsync(int userId)
    {
        var participants = await _unitOfWork.ConversationParticipants.GetByUserIdAsync(userId);
        var totalUnread = 0;

        foreach (var participant in participants)
        {
            var unread = await _unitOfWork.Messages.GetUnreadCountAsync(participant.ConversationId, userId);
            totalUnread += unread;
        }

        return OperationResult<int>.Success(totalUnread);
    }

    public async Task<OperationResult> AddParticipantAsync(int conversationId, int userId, int callerUserId)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult.Failure("Conversation not found");

        if (conversation.Type != ConversationType.Group)
            return OperationResult.Failure("Can only add participants to group conversations");

        var isCallerParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, callerUserId);
        if (!isCallerParticipant)
            return OperationResult.Failure("Caller is not a participant in this conversation");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted || !user.IsActive)
            return OperationResult.Failure("User not found or inactive");

        var alreadyParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, userId);
        if (alreadyParticipant)
            return OperationResult.Failure("User is already a participant");

        var participant = new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.ConversationParticipants.AddAsync(participant);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Participant added successfully");
    }

    public async Task<OperationResult> RemoveParticipantAsync(int conversationId, int userId, int callerUserId)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.IsDeleted)
            return OperationResult.Failure("Conversation not found");

        if (conversation.Type != ConversationType.Group)
            return OperationResult.Failure("Can only remove participants from group conversations");

        var isCallerParticipant = await _unitOfWork.ConversationParticipants.IsParticipantAsync(conversationId, callerUserId);
        if (!isCallerParticipant)
            return OperationResult.Failure("Caller is not a participant in this conversation");

        var participant = await _unitOfWork.ConversationParticipants
            .GetByConversationAndUserAsync(conversationId, userId);

        if (participant == null)
            return OperationResult.Failure("User is not a participant in this conversation");

        _unitOfWork.ConversationParticipants.SoftDelete(participant);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Participant removed successfully");
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