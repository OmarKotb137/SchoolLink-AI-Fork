using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Students;

public class UpdateStudentRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الطالب مطلوب")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "اسم الطالب يجب أن يكون بين 2 و 200 حرف")]
    public string FullName { get; set; } = string.Empty;

    public Gender? Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
}
