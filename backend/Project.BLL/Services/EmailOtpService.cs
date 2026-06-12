using System.Security.Cryptography;
using Common.Results;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class EmailOtpService : IEmailOtpService
{
    private const int CodeMinutesToLive = 10;
    private const int MaxAttempts = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public EmailOtpService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<OperationResult> SendVerificationOtpAsync(int userId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return OperationResult.Failure("البريد الإلكتروني مطلوب");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure("المستخدم غير موجود");

        var code = GenerateCode();
        var otp = new EmailOtp
        {
            UserId = userId,
            Email = normalizedEmail,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(CodeMinutesToLive),
            AttemptCount = 0
        };

        await _unitOfWork.EmailOtps.AddAsync(otp, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _emailService.SendEmailVerificationOtpAsync(normalizedEmail, user.FullName, code, ct);
        return OperationResult.Success("تم إرسال كود التحقق إلى البريد الإلكتروني");
    }

    public async Task<OperationResult> VerifyEmailOtpAsync(int userId, string email, string code, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return OperationResult.Failure("البريد الإلكتروني مطلوب");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure("المستخدم غير موجود");

        var otps = await _unitOfWork.EmailOtps.FindAsync(
            x => x.UserId == userId &&
                 x.Email == normalizedEmail &&
                 x.UsedAt == null &&
                 x.ExpiresAt > DateTime.UtcNow,
            ct);

        var otp = otps.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        if (otp == null)
            return OperationResult.Failure("كود التحقق غير صحيح أو منتهي الصلاحية");

        if (otp.AttemptCount >= MaxAttempts)
            return OperationResult.Failure("تم تجاوز عدد محاولات التحقق، اطلب كود جديد");

        otp.AttemptCount++;
        if (!BCrypt.Net.BCrypt.Verify(code, otp.CodeHash))
        {
            _unitOfWork.EmailOtps.Update(otp);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult.Failure("كود التحقق غير صحيح");
        }

        otp.UsedAt = DateTime.UtcNow;
        user.ContactEmail = normalizedEmail;
        user.IsContactEmailVerified = true;
        user.ContactEmailVerifiedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EmailOtps.Update(otp);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult.Success("تم تفعيل البريد الإلكتروني بنجاح");
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static string GenerateCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
