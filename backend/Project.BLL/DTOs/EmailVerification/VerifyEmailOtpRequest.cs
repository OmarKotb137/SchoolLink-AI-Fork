using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.EmailVerification;

public class VerifyEmailOtpRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits")]
    public string Code { get; set; } = string.Empty;
}
