using Common.Results;
using Project.BLL.DTOs.ChildProgress;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.Services;

public class ChildProgressService : IChildProgressService
{
    private readonly IUnitOfWork _unitOfWork;

    public ChildProgressService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<List<ChildProgressItemDto>>> GetChildProgressAsync(int parentUserId)
    {
        var currentYear = await _unitOfWork.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent && !y.IsDeleted);
        if (currentYear is null)
            return OperationResult<List<ChildProgressItemDto>>.Success(new List<ChildProgressItemDto>(), "لا توجد سنة دراسية حالية");

        var links = await _unitOfWork.ParentStudents.GetWithStudentDetailsByParentAsync(parentUserId);
        var activeLinks = links.Where(l => !l.IsDeleted && l.Student is { IsDeleted: false }).ToList();

        var results = new List<ChildProgressItemDto>();

        foreach (var link in activeLinks)
        {
            var student = link.Student!;
            var enrollment = student.Enrollments.FirstOrDefault(e => e.LeftAt == null && e.AcademicYearId == currentYear.Id && !e.IsDeleted);
            if (enrollment is null) continue;

            var className = enrollment.Class?.Name ?? "";
            var gradeName = enrollment.Class?.GradeLevel?.Name ?? "";

            var csts = await _unitOfWork.ClassSubjectTeachers
                .FindAsync(c => c.ClassId == enrollment.ClassId && c.AcademicYearId == currentYear.Id && !c.IsDeleted);
            var cstIds = csts.Select(c => c.Id).ToHashSet();

            // — Assignments —
            var assignments = await _unitOfWork.Assignments
                .FindAsync(a => cstIds.Contains(a.ClassSubjectTeacherId) && !a.IsDeleted && a.IsPublished);
            var submissions = await _unitOfWork.StudentAssignmentSubmissions.GetByEnrollmentIdAsync(enrollment.Id);
            var submissionMap = submissions.ToDictionary(s => s.AssignmentId);

            var assignmentDtos = assignments.Select(a =>
            {
                var cst = csts.FirstOrDefault(c => c.Id == a.ClassSubjectTeacherId);
                var subject = cst?.Subject?.Name ?? "";
                var sub = submissionMap.GetValueOrDefault(a.Id);

                string status;
                double? score;

                if (sub is not null)
                {
                    status = sub.IsGraded ? "submitted" : "submitted";
                    score = sub.IsGraded && sub.Score.HasValue ? (double)sub.Score.Value : null;
                }
                else if (a.DueDate.HasValue && a.DueDate.Value < DateTime.UtcNow)
                {
                    status = "late";
                    score = null;
                }
                else
                {
                    status = "not-submitted";
                    score = null;
                }

                return new AssignmentProgressDto
                {
                    Id = a.Id,
                    Subject = subject,
                    Title = a.Title,
                    Deadline = a.DueDate?.ToString("yyyy-MM-dd"),
                    Status = status,
                    Score = score,
                    MaxScore = (double)a.MaxScore,
                };
            }).ToList();

            // — Exams —
            var exams = await _unitOfWork.Exams
                .FindAsync(e => e.ClassSubjectTeacherId != null && cstIds.Contains(e.ClassSubjectTeacherId.Value) && !e.IsDeleted && e.IsPublished);
            var attempts = await _unitOfWork.StudentExamAttempts.GetByEnrollmentIdAsync(enrollment.Id);
            var attemptMap = attempts.ToDictionary(a => a.ExamId);

            var examDtos = exams.Select(e =>
            {
                var cst = csts.FirstOrDefault(c => c.Id == e.ClassSubjectTeacherId);
                var subject = cst?.Subject?.Name ?? "";
                var att = attemptMap.GetValueOrDefault(e.Id);

                string status;
                double? score;

                if (att is { IsGraded: true } && att.Score.HasValue)
                {
                    status = "done";
                    score = (double)att.Score.Value;
                }
                else if (e.StartTime.HasValue && e.StartTime.Value > DateTime.UtcNow)
                {
                    status = "upcoming";
                    score = null;
                }
                else if (att is not null)
                {
                    status = "done";
                    score = null;
                }
                else
                {
                    status = "missed";
                    score = null;
                }

                return new ExamProgressDto
                {
                    Id = e.Id,
                    Subject = subject,
                    Date = e.StartTime?.ToString("yyyy-MM-dd"),
                    Status = status,
                    Score = score,
                    MaxScore = (double)e.TotalScore,
                };
            }).ToList();

            // — Average score —
            var allPcts = new List<double>();
            allPcts.AddRange(submissions
                .Where(s => s.IsGraded && s.Score.HasValue && s.MaxScore > 0)
                .Select(s => (double)(s.Score!.Value / s.MaxScore) * 100));
            allPcts.AddRange(attempts
                .Where(a => a.IsGraded && a.Score.HasValue && a.TotalScore > 0)
                .Select(a => (double)(a.Score!.Value / a.TotalScore) * 100));
            var avgScore = allPcts.Count > 0 ? Math.Round(allPcts.Average(), 1) : 0;

            // — Attendance (based on actual recorded days, not hardcoded 180) —
            var allDays = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.EnrollmentId == enrollment.Id && !a.IsDeleted);
            var totalDays = allDays.Count;
            var absCount = allDays.Count(a => a.IsAbsent);
            var attendancePct = totalDays == 0 ? 100
                : Math.Round((double)(totalDays - absCount) / totalDays * 100, 1);

            results.Add(new ChildProgressItemDto
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                ClassName = className,
                GradeLevelName = gradeName,
                AvgScore = avgScore,
                AttendancePercentage = attendancePct,
                Assignments = assignmentDtos,
                Exams = examDtos,
            });
        }

        return OperationResult<List<ChildProgressItemDto>>.Success(results, "تم جلب بيانات متابعة الأبناء بنجاح");
    }
}
