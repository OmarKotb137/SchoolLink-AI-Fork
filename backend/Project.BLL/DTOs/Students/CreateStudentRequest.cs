using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Students;

public class CreateStudentRequest
{
    [Required(ErrorMessage = "اسم الطالب مطلوب")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "اسم الطالب يجب أن يكون بين 2 و 200 حرف")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يتكون من 14 رقماً")]
    [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يحتوي على أرقام فقط")]
    public string? NationalId { get; set; }
    
    public Gender? Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
}
