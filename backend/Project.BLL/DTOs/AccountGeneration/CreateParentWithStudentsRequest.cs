using Project.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.AccountGeneration;

public class CreateParentWithStudentsRequest
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "الاسم يجب أن يكون بين 2 و 100 حرف")]
    public string FullName { get; set; } = string.Empty;

    // اختياري — يُولَّد تلقائياً من الـ Backend إذا لم يُرسَل
    [StringLength(50, MinimumLength = 3)]
    public string? Username { get; set; }

    // اختياري — يُولَّد تلقائياً من الـ Backend إذا لم يُرسَل
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }

    // الـ unique key للـ dedup على مستوى الـ Parent
    [RegularExpression(@"^(01[0-2,5]\d{8}|0[2-9]\d{7,8})$", ErrorMessage = "رقم الهاتف المصري غير صحيح")]
    public string? Phone { get; set; }

    // اختياري تماماً — للإشعارات مستقبلاً
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string? ContactEmail { get; set; }

    public List<ParentChildLinkRequest> Children { get; set; } = new();
}

public class ParentChildLinkRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required]
    [Range(1, 5)]
    public RelationshipType Relationship { get; set; }
}
