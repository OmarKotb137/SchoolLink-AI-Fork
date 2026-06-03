using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.AIGenerationLog;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services
{
    public class AIGenerationLogService : IAIGenerationLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AIGenerationLogService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OperationResult<GetAIGenerationLogDto>> GetByIdAsync(int id)
        {
            var log = await _unitOfWork.AIGenerationLogs.GetByIdAsync(id);

            if (log == null || log.IsDeleted)
                return OperationResult<GetAIGenerationLogDto>.Failure("السجل غير موجود", 404);

            var dto = _mapper.Map<GetAIGenerationLogDto>(log);
            return OperationResult<GetAIGenerationLogDto>.Success(dto);
        }

        public async Task<OperationResult<List<GetAIGenerationLogDto>>> GetAllAsync()
        {
            var logs = await _unitOfWork.AIGenerationLogs.GetAllAsync();

            var dtos = _mapper.Map<List<GetAIGenerationLogDto>>(logs);
            return OperationResult<List<GetAIGenerationLogDto>>.Success(dtos);
        }

        public async Task<OperationResult<List<GetAIGenerationLogDto>>> GetByUserIdAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return OperationResult<List<GetAIGenerationLogDto>>.Failure("المستخدم غير موجود", 404);

            var logs = await _unitOfWork.AIGenerationLogs.GetByUserIdAsync(userId);

            var dtos = _mapper.Map<List<GetAIGenerationLogDto>>(logs);
            return OperationResult<List<GetAIGenerationLogDto>>.Success(dtos);
        }

        public async Task<OperationResult<GetAIGenerationLogDto>> CreateAsync(CreateAIGenerationLogDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);

            if (user == null || user.IsDeleted)
                return OperationResult<GetAIGenerationLogDto>.Failure("المستخدم غير موجود", 404);

            var validTypes = new[] { "exam_generation", "grading", "chatbot", "analysis", "study_plan" };
            if (!validTypes.Contains(dto.OperationType))
                return OperationResult<GetAIGenerationLogDto>.Failure("نوع العملية غير صالح", 400);

            if (dto.TokensUsed.HasValue && dto.TokensUsed < 0)
                return OperationResult<GetAIGenerationLogDto>.Failure("يجب أن تكون قيمة TokensUsed غير سالبة", 400);

            if (dto.LatencyMs.HasValue && dto.LatencyMs < 0)
                return OperationResult<GetAIGenerationLogDto>.Failure("يجب أن تكون قيمة LatencyMs غير سالبة", 400);

            var log = _mapper.Map<AIGenerationLog>(dto);

            await _unitOfWork.AIGenerationLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var resultDto = _mapper.Map<GetAIGenerationLogDto>(log);
            resultDto.UserName = user.FullName;

            return OperationResult<GetAIGenerationLogDto>.Success(resultDto, "تم إنشاء السجل بنجاح");
        }

        public async Task<OperationResult<AIGenerationLogSummaryDto>> GetSummaryAsync()
        {
            var totalCalls = await _unitOfWork.AIGenerationLogs.GetCallCountAsync();
            var successRate = await _unitOfWork.AIGenerationLogs.GetSuccessRateAsync();
            var totalTokens = await _unitOfWork.AIGenerationLogs.GetTotalTokensUsedAsync();
            var avgLatency = await _unitOfWork.AIGenerationLogs.GetAverageLatencyAsync(string.Empty);

            var summary = new AIGenerationLogSummaryDto
            {
                TotalRequests = totalCalls,
                SuccessCount = (int)(totalCalls * successRate / 100),
                FailureCount = totalCalls - (int)(totalCalls * successRate / 100),
                TotalTokensUsed = totalTokens,
                AverageLatencyMs = avgLatency
            };

            return OperationResult<AIGenerationLogSummaryDto>.Success(summary);
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var log = await _unitOfWork.AIGenerationLogs.GetByIdAsync(id);
            if (log == null || log.IsDeleted)
                return OperationResult.Failure("السجل غير موجود", 404);

            _unitOfWork.AIGenerationLogs.SoftDelete(log);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("تم حذف السجل بنجاح");
        }

        public async Task<OperationResult> DeleteOlderThanAsync(DateTime cutoff)
        {
            var all = await _unitOfWork.AIGenerationLogs.GetByDateRangeAsync(DateTime.MinValue, cutoff);
            var toDelete = all.Where(l => !l.IsDeleted).ToList();

            if (toDelete.Count == 0)
                return OperationResult.Success("لا توجد سجلات للحذف");

            _unitOfWork.AIGenerationLogs.SoftDeleteRange(toDelete);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success($"تم حذف {toDelete.Count} سجل/سجلات بنجاح");
        }
    }
}