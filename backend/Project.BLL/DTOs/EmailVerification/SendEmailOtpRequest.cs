using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.EmailVerification;

public class SendEmailOtpRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;
}
