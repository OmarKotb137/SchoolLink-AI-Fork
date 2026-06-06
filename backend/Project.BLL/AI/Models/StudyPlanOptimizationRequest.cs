namespace Project.BLL.AI.Models;

public class StudyPlanOptimizationRequest
{
    public int EnrollmentId { get; set; }
    public int AvailableDays { get; set; } = 7;
    public int HoursPerDay { get; set; } = 3;
    public List<string> WeakSubjects { get; set; } = new();
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
