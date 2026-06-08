namespace Project.BLL.DTOs.StudyPlans;

public class CreateStudyPlanRequest
{
    public int EnrollmentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? RestDay { get; set; }
    public List<CreateStudyPlanItemRequest> Items { get; set; } = new();
}
