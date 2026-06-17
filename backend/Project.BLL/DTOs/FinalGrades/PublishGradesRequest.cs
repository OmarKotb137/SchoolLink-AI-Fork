using Project.Domain.Enums;

namespace Project.BLL.DTOs.FinalGrades;

public class PublishGradesRequest
{
    public int AcademicYearId { get; set; }
    public int? ClassId { get; set; }
    public int PublishedById { get; set; }
    public AcademicTerm? Term { get; set; }
}
