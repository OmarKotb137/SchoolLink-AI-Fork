using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Common.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Project.BLL.DTOs.Auth;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Enums;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return OperationResult<AuthResponseDto>.Failure("البريد الإلكتروني أو كلمة المرور غير صحيحة");

        if (user.IsDeleted)
            return OperationResult<AuthResponseDto>.Failure("تم حذف هذا الحساب");

        if (!user.IsActive)
            return OperationResult<AuthResponseDto>.Failure("هذا الحساب غير نشط. اتصل بالمسؤول");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return OperationResult<AuthResponseDto>.Failure("البريد الإلكتروني أو كلمة المرور غير صحيحة");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            Expiry = DateTime.UtcNow.AddMinutes(GetConfigDouble("Jwt:ExpiryInMinutes", 60)),
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role.ToString()
        }, "تم تسجيل الدخول بنجاح");
    }

    public async Task<OperationResult<AuthResponseDto>> LoginByRoleAsync(LoginRequest request, UserRole role)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return OperationResult<AuthResponseDto>.Failure("البريد الإلكتروني أو كلمة المرور غير صحيحة");

        if (user.IsDeleted)
            return OperationResult<AuthResponseDto>.Failure("تم حذف هذا الحساب");

        if (!user.IsActive)
            return OperationResult<AuthResponseDto>.Failure("هذا الحساب غير نشط. اتصل بالمسؤول");

        if (user.Role != role)
            return OperationResult<AuthResponseDto>.Failure("بيانات الدخول غير صحيحة لهذا النوع من المستخدمين");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return OperationResult<AuthResponseDto>.Failure("البريد الإلكتروني أو كلمة المرور غير صحيحة");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            Expiry = DateTime.UtcNow.AddMinutes(GetConfigDouble("Jwt:ExpiryInMinutes", 60)),
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role.ToString()
        }, "تم تسجيل الدخول بنجاح");
    }

    public async Task<OperationResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (storedToken == null)
            return OperationResult<AuthResponseDto>.Failure("رمز التحديث غير صالح");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return OperationResult<AuthResponseDto>.Failure("انتهت صلاحية رمز التحديث");

        if (storedToken.IsRevoked)
            return OperationResult<AuthResponseDto>.Failure("تم إلغاء رمز التحديث");

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user == null || user.IsDeleted || !user.IsActive)
            return OperationResult<AuthResponseDto>.Failure("حساب المستخدم غير متاح");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            Expiry = DateTime.UtcNow.AddMinutes(GetConfigDouble("Jwt:ExpiryInMinutes", 60)),
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role.ToString()
        }, "تم تحديث الرمز بنجاح");
    }

    public async Task<OperationResult> LogoutAsync(LogoutRequest request, int callerUserId)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (storedToken == null)
            return OperationResult.Failure("رمز التحديث غير موجود");

        if (storedToken.UserId != callerUserId)
            return OperationResult.Failure("رمز التحديث لا ينتمي إلى المستخدم الحالي");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم تسجيل الخروج بنجاح");
    }

    public async Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure("المستخدم غير موجود");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return OperationResult.Failure("كلمة المرور الحالية غير صحيحة");

        if (request.NewPassword != request.ConfirmNewPassword)
            return OperationResult.Failure("كلمة المرور الجديدة وتأكيدها غير متطابقين");

        if (request.NewPassword.Length < 6)
            return OperationResult.Failure("يجب أن تتكون كلمة المرور الجديدة من 6 أحرف على الأقل");

        if (request.CurrentPassword == request.NewPassword)
            return OperationResult.Failure("يجب أن تختلف كلمة المرور الجديدة عن كلمة المرور الحالية");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.RefreshTokens.RevokeAllForUserAsync(request.UserId);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم تغيير كلمة المرور بنجاح");
    }

    private string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var expiry = DateTime.UtcNow.AddMinutes(GetConfigDouble("Jwt:ExpiryInMinutes", 60));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateAndStoreRefreshTokenAsync(int userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(
                GetConfigDouble("Jwt:RefreshTokenExpiryInDays", 7)),
            IsRevoked = false
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return refreshToken;
    }

    private double GetConfigDouble(string key, double defaultValue)
    {
        var value = _configuration[key];
        return double.TryParse(value, out var result) ? result : defaultValue;
    }
}