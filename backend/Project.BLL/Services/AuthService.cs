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
            return OperationResult<AuthResponseDto>.Failure("Invalid email or password");

        if (user.IsDeleted)
            return OperationResult<AuthResponseDto>.Failure("This account has been deleted");

        if (!user.IsActive)
            return OperationResult<AuthResponseDto>.Failure("This account is inactive. Contact an administrator");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return OperationResult<AuthResponseDto>.Failure("Invalid email or password");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            Expiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60")),
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role.ToString()
        }, "Login successful");
    }

    public async Task<OperationResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (storedToken == null)
            return OperationResult<AuthResponseDto>.Failure("Invalid refresh token");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return OperationResult<AuthResponseDto>.Failure("Refresh token has expired");

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user == null || user.IsDeleted || !user.IsActive)
            return OperationResult<AuthResponseDto>.Failure("User account is unavailable");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            Expiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60")),
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role.ToString()
        }, "Token refreshed successfully");
    }

    public async Task<OperationResult> LogoutAsync(LogoutRequest request)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (storedToken == null)
            return OperationResult.Failure("Refresh token not found");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Logged out successfully");
    }

    public async Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null || user.IsDeleted)
            return OperationResult.Failure("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return OperationResult.Failure("Current password is incorrect");

        if (request.NewPassword != request.ConfirmNewPassword)
            return OperationResult.Failure("New password and confirmation do not match");

        if (request.NewPassword.Length < 6)
            return OperationResult.Failure("New password must be at least 6 characters");

        if (request.CurrentPassword == request.NewPassword)
            return OperationResult.Failure("New password must differ from current password");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.RefreshTokens.RevokeAllForUserAsync(request.UserId);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("Password changed successfully");
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

        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"] ?? "60"));

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
                double.Parse(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7")),
            IsRevoked = false
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return refreshToken;
    }
}