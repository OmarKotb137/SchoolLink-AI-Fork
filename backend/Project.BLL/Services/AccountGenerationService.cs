using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.AccountGeneration;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.BLL.Utils;

namespace Project.BLL.Services;

public class AccountGenerationService : IAccountGenerationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AccountGenerationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<IEnumerable<StudentAccountCandidateDto>>> GetStudentAccountCandidatesAsync()
    {
        var candidates = await _unitOfWork.Students.GetWithoutUserAccountAsync();

        var dtos = candidates
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.FullName)
            .Select(s => new StudentAccountCandidateDto
            {
                StudentId = s.Id,
                FullName = s.FullName,
                NationalId = s.NationalId,
                Gender = s.Gender?.ToString(),
                CreatedAt = s.CreatedAt
            })
            .ToList();

        return OperationResult<IEnumerable<StudentAccountCandidateDto>>.Success(dtos);
    }

    public async Task<OperationResult<GenerateStudentAccountResultDto>> GenerateStudentAccountAsync(int studentId)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<GenerateStudentAccountResultDto>.Failure("الطالب غير موجود");

        if (student.UserId != null)
            return OperationResult<GenerateStudentAccountResultDto>.Failure("هذا الطالب مرتبط بحساب بالفعل");

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var username = await GenerateUniqueUsernameAsync('s');
            var plainPassword = PasswordGenerator.Generate();

            var user = new User
            {
                FullName     = student.FullName,
                Username     = username,
                ContactEmail = null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role         = UserRole.Student,
                IsActive     = true
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            student.UserId = user.Id;
            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            return OperationResult<GenerateStudentAccountResultDto>.Success(
                new GenerateStudentAccountResultDto
                {
                    StudentId       = student.Id,
                    StudentName     = student.FullName,
                    GeneratedUsername = username,
                    PlainPassword   = plainPassword,
                    Success         = true
                },
                "تم إنشاء حساب الطالب بنجاح");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OperationResult<GenerateBulkStudentAccountsResultDto>> GenerateBulkStudentAccountsAsync(List<int> studentIds)
    {
        if (studentIds == null || studentIds.Count == 0)
            return OperationResult<GenerateBulkStudentAccountsResultDto>.Failure("يجب تحديد طالب واحد على الأقل");

        studentIds = studentIds.Distinct().ToList();

        var requestedStudents = await _unitOfWork.Students.FindAsync(s => studentIds.Contains(s.Id));
        var studentNamesById = requestedStudents.ToDictionary(s => s.Id, s => s.FullName);

        var result = new GenerateBulkStudentAccountsResultDto
        {
            TotalRequested = studentIds.Count
        };

        foreach (var studentId in studentIds)
        {
            try
            {
                var singleResult = await GenerateStudentAccountAsync(studentId);
                if (singleResult.IsSuccess && singleResult.Data != null)
                {
                    result.SuccessCount++;
                    result.Results.Add(singleResult.Data);
                    continue;
                }

                result.FailureCount++;
                result.Results.Add(new GenerateStudentAccountResultDto
                {
                    StudentId     = studentId,
                    StudentName   = studentNamesById.GetValueOrDefault(studentId, "غير معروف"),
                    Success       = false,
                    ErrorMessage  = singleResult.Message
                });
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Results.Add(new GenerateStudentAccountResultDto
                {
                    StudentId     = studentId,
                    StudentName   = studentNamesById.GetValueOrDefault(studentId, "غير معروف"),
                    Success       = false,
                    ErrorMessage  = $"خطأ غير متوقع: {ex.Message}"
                });
            }
        }

        return OperationResult<GenerateBulkStudentAccountsResultDto>.Success(
            result,
            $"تم إنشاء {result.SuccessCount} من {result.TotalRequested} حساب بنجاح");
    }

    public async Task<OperationResult<CreateParentWithStudentsResultDto>> CreateParentWithStudentsAsync(CreateParentWithStudentsRequest request)
    {
        // توليد Username تلقائياً لو مش موجود
        var username = !string.IsNullOrWhiteSpace(request.Username)
            ? request.Username.Trim().ToLower()
            : await GenerateUniqueUsernameAsync('p');

        // فحص تكرار الـ Username
        var existingByUsername = await _unitOfWork.Users.GetByUsernameAsync(username);
        if (existingByUsername != null && !existingByUsername.IsDeleted)
            return OperationResult<CreateParentWithStudentsResultDto>.Failure("اسم المستخدم مأخوذ بالفعل");

        if (existingByUsername != null && existingByUsername.IsDeleted)
            return OperationResult<CreateParentWithStudentsResultDto>.Failure("اسم المستخدم مرتبط بحساب محذوف، يرجى اختيار اسم آخر");

        // فحص تكرار بالتليفون (لو موجود)
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var existingByPhone = await _unitOfWork.Users.GetParentByPhoneAsync(request.Phone);
            if (existingByPhone != null)
                return OperationResult<CreateParentWithStudentsResultDto>.Failure(
                    $"يوجد ولي أمر بنفس رقم الهاتف بالفعل (اسم المستخدم: {existingByPhone.Username}). استخدم ربط الطالب بحسابه الموجود.");
        }

        // توليد Password تلقائياً لو مش موجود
        var password = !string.IsNullOrWhiteSpace(request.Password)
            ? request.Password
            : PasswordGenerator.Generate();

        request.Children = (request.Children ?? [])
            .GroupBy(c => c.StudentId)
            .Select(g => g.First())
            .ToList();

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var user = new User
            {
                FullName     = request.FullName,
                Username     = username,
                ContactEmail = request.ContactEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Phone        = request.Phone,
                Role         = UserRole.Parent,
                IsActive     = true
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var linkResults = new List<ChildLinkResultDto>();

            foreach (var child in request.Children)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(child.StudentId);
                if (student == null || student.IsDeleted)
                {
                    linkResults.Add(new ChildLinkResultDto
                    {
                        StudentId    = child.StudentId,
                        StudentName  = "غير معروف",
                        Success      = false,
                        ErrorMessage = "الطالب غير موجود"
                    });
                    continue;
                }

                await _unitOfWork.ParentStudents.AddAsync(new ParentStudent
                {
                    ParentId     = user.Id,
                    StudentId    = child.StudentId,
                    Relationship = child.Relationship
                });

                linkResults.Add(new ChildLinkResultDto
                {
                    StudentId   = child.StudentId,
                    StudentName = student.FullName,
                    Success     = true
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var userDto = _mapper.Map<UserDto>(user);
            return OperationResult<CreateParentWithStudentsResultDto>.Success(
                new CreateParentWithStudentsResultDto
                {
                    Parent       = userDto,
                    LinkedCount  = linkResults.Count(r => r.Success),
                    FailedCount  = linkResults.Count(r => !r.Success),
                    LinkResults  = linkResults
                },
                "تم إنشاء حساب ولي الأمر بنجاح");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OperationResult<ParentPhoneCheckDto>> CheckParentPhoneAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return OperationResult<ParentPhoneCheckDto>.Failure("رقم الهاتف مطلوب");

        var existing = await _unitOfWork.Users.GetParentByPhoneAsync(phone);
        if (existing == null)
            return OperationResult<ParentPhoneCheckDto>.Success(
                new ParentPhoneCheckDto { AlreadyExists = false });

        return OperationResult<ParentPhoneCheckDto>.Success(new ParentPhoneCheckDto
        {
            AlreadyExists          = true,
            ExistingParentId       = existing.Id,
            ExistingParentName     = existing.FullName,
            ExistingParentUsername = existing.Username
        });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<string> GenerateUniqueUsernameAsync(char prefix)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var username = UsernameGenerator.Generate(prefix);
            var existing = await _unitOfWork.Users.GetByUsernameAsync(username);
            if (existing == null) return username;
        }

        throw new InvalidOperationException("تعذر توليد username فريد بعد 10 محاولات");
    }
}
