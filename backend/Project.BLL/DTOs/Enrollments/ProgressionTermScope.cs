namespace Project.BLL.DTOs.Enrollments;

/// <summary>
/// نطاق الترم الذي يُبنى عليه قرار نهاية العام.
/// يختاره الأدمن في كل مرة من شاشة Student Progression.
/// </summary>
public enum ProgressionTermScope
{
    /// <summary>الترم الأول فقط.</summary>
    FirstSemester = 1,

    /// <summary>الترم الثاني فقط.</summary>
    SecondSemester = 2,

    /// <summary>الترمين مجتمعين (متوسط/مجموع الترمين) — الأساس الأنسب لقرار نهاية العام.</summary>
    BothSemesters = 3
}
