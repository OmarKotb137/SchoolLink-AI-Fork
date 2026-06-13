using Common.Results;

namespace Project.BLL.Interfaces;

public interface IEmailOtpService
{
    Task<OperationResult> SendVerificationOtpAsync(int userId, string email, CancellationToken ct = default);
    Task<OperationResult> VerifyEmailOtpAsync(int userId, string email, string code, CancellationToken ct = default);
}
