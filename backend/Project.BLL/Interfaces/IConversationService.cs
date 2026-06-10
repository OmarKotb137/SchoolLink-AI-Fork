using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Conversations;

namespace Project.BLL.Interfaces;

public interface IConversationService
{
    Task<OperationResult<ConversationDto>> CreateDirectConversationAsync(CreateDirectConversationRequest request);
    Task<OperationResult<ConversationDto>> CreateGroupConversationAsync(CreateGroupConversationRequest request);
    Task<OperationResult<ConversationDto>> CreateSubjectGroupConversationAsync(CreateSubjectGroupConversationRequest request);
    Task<OperationResult<ConversationDto>> CreateClassGroupConversationAsync(CreateClassGroupConversationRequest request);
    Task<OperationResult<ConversationDto>> GetConversationByIdAsync(int conversationId, int requestingUserId);
    Task<OperationResult<IEnumerable<ConversationDto>>> GetUserConversationsAsync(int userId);
    Task<OperationResult<int>> GetUnreadMessagesCountAsync(int userId);
    Task<OperationResult<MessageDto>> SendMessageAsync(SendMessageRequest request);
    Task<OperationResult<PagedResult<MessageDto>>> GetMessagesAsync(int conversationId, int requestingUserId, PaginationFilter filter);
    Task<OperationResult<MessageDto>> UpdateMessageAsync(int messageId, int userId, string newContent);
    Task<OperationResult<string?>> DeleteMessageAsync(int messageId, int userId);
    Task<OperationResult<MessageDto>> GetMessageByIdAsync(int messageId);
    Task<OperationResult<MessageDto>> TranscribeMessageAsync(int messageId, int userId, string voiceText);
    Task<OperationResult> BlockUserAsync(int conversationId, int blockerId, int blockedUserId);
    Task<OperationResult> UnblockUserAsync(int conversationId, int blockerId, int blockedUserId);
    Task<OperationResult<bool>> IsUserBlockedAsync(int conversationId, int userId, int otherUserId);
    Task<OperationResult> AddParticipantAsync(int conversationId, int userId, int callerUserId);
    Task<OperationResult> RemoveParticipantAsync(int conversationId, int userId, int callerUserId);
    Task<OperationResult> DeleteConversationAsync(int conversationId, int userId);
    Task<OperationResult<ConversationDto>> UpdateConversationTitleAsync(int conversationId, string title, int userId);
    Task<OperationResult<IEnumerable<ConversationParticipantDto>>> GetConversationParticipantsAsync(int conversationId, int userId);
    Task<OperationResult> MarkConversationAsReadAsync(int conversationId, int userId);
    Task<OperationResult<IEnumerable<ConversationDto>>> SearchConversationsAsync(int userId, string term);
}
