using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.SchoolProfiles;

public class UpdateSchoolProfileRequest
{
    [Required(ErrorMessage = "School name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "School name must be between 2 and 200 characters")]
    public string SchoolName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Governorate is required")]
    [StringLength(100, ErrorMessage = "Governorate cannot exceed 100 characters")]
    public string Governorate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Directorate is required")]
    [StringLength(150, ErrorMessage = "Directorate cannot exceed 150 characters")]
    public string Directorate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Educational administration is required")]
    [StringLength(150, ErrorMessage = "Educational administration cannot exceed 150 characters")]
    public string EducationalAdministration { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
    public string? Address { get; set; }

    [RegularExpression(@"^(01[0-2,5]\d{8}|0[2-9]\d{7,8})$", ErrorMessage = "Invalid Egyptian phone number")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    public string? Email { get; set; }

    [StringLength(150, ErrorMessage = "Manager name cannot exceed 150 characters")]
    public string? ManagerName { get; set; }

    [StringLength(500, ErrorMessage = "Logo path cannot exceed 500 characters")]
    public string? LogoPath { get; set; }

    public bool IsActive { get; set; } = true;
}
