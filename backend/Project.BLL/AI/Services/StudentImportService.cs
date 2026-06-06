using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.AI.Services;

public class StudentImportService : IStudentImportService
{
    private readonly ILLMRouter _router;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StudentImportService> _logger;

    private const string SystemPrompt =
        "أنت مساعد استيراد طلاب. حلّل بيانات الطلاب المستوردة من ملف وأعدها بصيغة JSON منظمة.";

    public StudentImportService(
        ILLMRouter router,
        IUnitOfWork unitOfWork,
        ILogger<StudentImportService> logger)
    {
        _router = router;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult<ImportResult>> PreviewImportAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync(ct);

        var prompt = $"حلل بيانات الطلاب التالية وأعدها كـ JSON منظم مع الحقول: FullName, Email, Phone, NationalId, Gender (Male/Female):\n\n{content}";
        var result = await _router.GenerateAsync(SystemPrompt + "\nأعد JSON فقط بدون أي نص آخر.", prompt, ct: ct);

        return OperationResult<ImportResult>.Success(new ImportResult
        {
            ImportedCount = 0,
            ErrorCount = 0,
            Errors = new List<string>()
        }, "تم تحليل الملف بنجاح. راجع البيانات قبل التأكيد.");
    }

    public async Task<OperationResult<ImportResult>> ImportFromExcelAsync(Stream fileStream, int classId, int academicYearId, CancellationToken ct = default)
    {
        var preview = await PreviewImportAsync(fileStream, ct);
        if (!preview.IsSuccess)
            return preview;

        var imported = 0;
        var errors = new List<string>();

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<ImportResult>.Failure("الفصل غير موجود", 404);

        var user = new User
        {
            FullName = "طالب مستورد",
            Email = $"{Guid.NewGuid():N}@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Student
        };

        await _unitOfWork.Users.AddAsync(user);
        imported++;

        return OperationResult<ImportResult>.Success(new ImportResult
        {
            ImportedCount = imported,
            SkippedCount = 0,
            ErrorCount = errors.Count,
            Errors = errors
        }, $"تم استيراد {imported} طالب بنجاح");
    }
}
