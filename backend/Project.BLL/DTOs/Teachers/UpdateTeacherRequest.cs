using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Teachers;

public class UpdateTeacherRequest
{
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^(01[0-2,5]\d{8}|0[2-9]\d{7,8})$", ErrorMessage = "Invalid Egyptian phone number")]
    public string? Phone { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public List<int> SubjectIds { get; set; } = new();
}
