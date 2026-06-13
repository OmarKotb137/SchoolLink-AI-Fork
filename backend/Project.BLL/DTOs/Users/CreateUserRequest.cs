using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Users;

public class CreateUserRequest
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "الاسم يجب أن يكون بين 2 و 100 حرف")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "اسم المستخدم يجب أن يكون بين 3 و 50 حرفاً")]
    [RegularExpression(@"^[a-z0-9._]+$", ErrorMessage = "اسم المستخدم يقبل أحرفاً صغيرة وأرقام ونقاط وشرطات سفلية فقط")]
    public string Username { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [StringLength(200, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 200 حرف")]
    public string? ContactEmail { get; set; }

    [RegularExpression(@"^(01[0-2,5]\d{8}|0[2-9]\d{7,8})$", ErrorMessage = "رقم الهاتف المصري غير صحيح")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "الدور مطلوب")]
    public UserRole Role { get; set; }
}
