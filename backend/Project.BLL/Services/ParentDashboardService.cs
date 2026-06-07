using Common.Results;
using Project.BLL.DTOs.Dashboard;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class ParentDashboardService : IParentDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public ParentDashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<ParentDashboardDto>> GetParentDashboardAsync(int parentId)
    {
        var dto = new ParentDashboardDto();

        var links = await _unitOfWork.ParentStudents.GetWithStudentDetailsByParentAsync(parentId);
        var activeLinks = links.Where(l => !l.IsDeleted && !l.Student.IsDeleted).ToList();

        foreach (var link in activeLinks)
        {
            var student = link.Student;
            var enrollment = student.Enrollments.FirstOrDefault(e => e.LeftAt == null);
            if (enrollment == null) continue;

            var className = enrollment.Class?.Name ?? "";
            var gradeName = enrollment.Class?.GradeLevel?.Name ?? "";

            var absences = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.EnrollmentId == enrollment.Id && a.IsAbsent && !a.IsDeleted);
            var absCount = absences.Count;

            var assessments = await _unitOfWork.PeriodicAssessments
                .FindAsync(pa => pa.EnrollmentId == enrollment.Id);
            var last = assessments.OrderByDescending(a => a.AssessmentDate).FirstOrDefault();
            var lastScore = last != null ? $"{last.Score}/{last.MaxScore}" : "—";

            var periodAvgs = await _unitOfWork.PeriodAverages
                .FindAsync(pa => pa.EnrollmentId == enrollment.Id);
            var totalPct = periodAvgs.Any()
                ? $"{Math.Round(periodAvgs.Average(a => (double)(a.AvgScore / a.MaxScore) * 100), 1)}%"
                : "—";

            var performance = periodAvgs.Any()
                ? (decimal)Math.Round(periodAvgs.Average(a => (double)(a.AvgScore / a.MaxScore) * 100), 1)
                : 0;

            dto.Children.Add(new ParentChildDto
            {
                Name = student.FullName,
                Grade = gradeName,
                Class = className,
                Performance = performance,
                Grades = new ChildGradesDto
                {
                    Last = lastScore,
                    Total = totalPct
                },
                Absences = absCount
            });

            dto.RecentActivities.Add($"تم تحديث درجات {student.FullName} في التقييم الأسبوعي");
        }

        dto.RecentActivities.AddRange(new[]
        {
            "تم تحديث جدول الأبناء للعام الدراسي",
            "متوفر تقرير الأداء الشهري"
        });

        return OperationResult<ParentDashboardDto>.Success(dto, "تم جلب بيانات dashboard ولي الأمر بنجاح");
    }
}
