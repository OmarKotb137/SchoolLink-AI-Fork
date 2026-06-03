using Common.Results;
using Project.BLL.DTOs.Auth;

namespace Project.BLL.Interfaces;

public interface IAuthService
{
    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequest request);
    Task<OperationResult<AuthResponseDto>> LoginByRoleAsync(LoginRequest request, Project.Domain.Enums.UserRole role);
    Task<OperationResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<OperationResult> LogoutAsync(LogoutRequest request, int callerUserId);
    Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request);
}
