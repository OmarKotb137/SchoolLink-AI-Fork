using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Conversations;

namespace Project.BLL.Interfaces;

public interface IConversationService
{
    Task<OperationResult<ConversationDto>> CreateDirectConversationAsync(CreateDirectConversationRequest request);
    Task<OperationResult<ConversationDto>> CreateGroupConversationAsync(CreateGroupConversationRequest request);
    Task<OperationResult<ConversationDto>> CreateSubjectGroupConversationAsync(CreateSubjectGroupConversationRequest request);
    Task<OperationResult<IEnumerable<ConversationDto>>> GetUserConversationsAsync(int userId);
    Task<OperationResult<int>> GetUnreadMessagesCountAsync(int userId);
    Task<OperationResult<MessageDto>> SendMessageAsync(SendMessageRequest request);
    Task<OperationResult<PagedResult<MessageDto>>> GetMessagesAsync(int conversationId, int requestingUserId, PaginationFilter filter);
    Task<OperationResult> AddParticipantAsync(int conversationId, int userId, int callerUserId);
    Task<OperationResult> RemoveParticipantAsync(int conversationId, int userId, int callerUserId);
}
