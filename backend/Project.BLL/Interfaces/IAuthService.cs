using Common.Results;
using Project.BLL.DTOs.Auth;

namespace Project.BLL.Interfaces;

public interface IAuthService
{
    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequest request);
    Task<OperationResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<OperationResult> LogoutAsync(LogoutRequest request);
    Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request);
}
