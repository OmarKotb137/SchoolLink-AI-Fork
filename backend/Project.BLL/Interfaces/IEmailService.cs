namespace Project.BLL.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationOtpAsync(string toEmail, string toName, string code, CancellationToken ct = default);
}
