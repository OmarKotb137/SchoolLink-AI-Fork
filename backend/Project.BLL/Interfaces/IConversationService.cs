using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Conversations;

namespace Project.BLL.Interfaces;

public interface IConversationService
{
    Task<OperationResult<ConversationDto>> CreateDirectConversationAsync(CreateDirectConversationRequest request);
    Task<OperationResult<ConversationDto>> CreateGroupConversationAsync(CreateGroupConversationRequest request);
    Task<OperationResult<MessageDto>> SendMessageAsync(SendMessageRequest request);
    Task<OperationResult<PagedResult<MessageDto>>> GetMessagesAsync(int conversationId, int requestingUserId, PaginationFilter filter);
}
