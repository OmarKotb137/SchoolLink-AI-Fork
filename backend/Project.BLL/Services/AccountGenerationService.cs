using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.AccountGeneration;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Security.Cryptography;

namespace Project.BLL.Services;

public class AccountGenerationService : IAccountGenerationService
{
    private static readonly char[] EmailChars = "abcdefghjkmnpqrstuvwxyz23456789".ToCharArray();
    private static readonly char[] PasswordUpper = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] PasswordLower = "abcdefghjkmnpqrstuvwxyz".ToCharArray();
    private static readonly char[] PasswordDigits = "23456789".ToCharArray();
    private static readonly char[] PasswordAll = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789".ToCharArray();

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

            var email = await GenerateUniqueStudentEmailAsync();
            var plainPassword = GenerateSecurePassword();

            var user = new User
            {
                FullName = student.FullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role = UserRole.Student,
                IsActive = true
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
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    GeneratedEmail = email,
                    PlainPassword = plainPassword,
                    Success = true
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
                    StudentId = studentId,
                    StudentName = studentNamesById.GetValueOrDefault(studentId, "غير معروف"),
                    Success = false,
                    ErrorMessage = singleResult.Message
                });
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Results.Add(new GenerateStudentAccountResultDto
                {
                    StudentId = studentId,
                    StudentName = studentNamesById.GetValueOrDefault(studentId, "غير معروف"),
                    Success = false,
                    ErrorMessage = $"خطأ غير متوقع: {ex.Message}"
                });
            }
        }

        return OperationResult<GenerateBulkStudentAccountsResultDto>.Success(
            result,
            $"تم إنشاء {result.SuccessCount} من {result.TotalRequested} حساب بنجاح");
    }

    public async Task<OperationResult<CreateParentWithStudentsResultDto>> CreateParentWithStudentsAsync(CreateParentWithStudentsRequest request)
    {
        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existing != null && !existing.IsDeleted)
            return OperationResult<CreateParentWithStudentsResultDto>.Failure("يوجد مستخدم مسجل بهذا البريد الإلكتروني بالفعل");

        if (existing != null && existing.IsDeleted)
            return OperationResult<CreateParentWithStudentsResultDto>.Failure("هذا البريد الإلكتروني مرتبط بحساب محذوف، يرجى استخدام بريد إلكتروني آخر");

        request.Children = (request.Children ?? [])
            .GroupBy(c => c.StudentId)
            .Select(g => g.First())
            .ToList();

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone = request.Phone,
                Role = UserRole.Parent,
                IsActive = true
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
                        StudentId = child.StudentId,
                        StudentName = "غير معروف",
                        Success = false,
                        ErrorMessage = "الطالب غير موجود"
                    });
                    continue;
                }

                await _unitOfWork.ParentStudents.AddAsync(new ParentStudent
                {
                    ParentId = user.Id,
                    StudentId = child.StudentId,
                    Relationship = child.Relationship
                });

                linkResults.Add(new ChildLinkResultDto
                {
                    StudentId = child.StudentId,
                    StudentName = student.FullName,
                    Success = true
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var userDto = _mapper.Map<UserDto>(user);
            return OperationResult<CreateParentWithStudentsResultDto>.Success(
                new CreateParentWithStudentsResultDto
                {
                    Parent = userDto,
                    LinkedCount = linkResults.Count(r => r.Success),
                    FailedCount = linkResults.Count(r => !r.Success),
                    LinkResults = linkResults
                },
                "تم إنشاء حساب ولي الأمر بنجاح");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private static string GenerateSecurePassword()
    {
        var bytes = new byte[10];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[10];
        chars[0] = PasswordUpper[bytes[0] % PasswordUpper.Length];
        chars[1] = PasswordLower[bytes[1] % PasswordLower.Length];
        chars[2] = PasswordDigits[bytes[2] % PasswordDigits.Length];

        for (var i = 3; i < chars.Length; i++)
            chars[i] = PasswordAll[bytes[i] % PasswordAll.Length];

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    private async Task<string> GenerateUniqueStudentEmailAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);
            var suffix = new string(bytes.Select(b => EmailChars[b % EmailChars.Length]).ToArray());
            var email = $"s{suffix}@students.schoollink.local";

            var existing = await _unitOfWork.Users.GetByEmailAsync(email);
            if (existing == null)
                return email;
        }

        throw new InvalidOperationException("تعذر توليد بريد إلكتروني فريد بعد 10 محاولات");
    }
}
