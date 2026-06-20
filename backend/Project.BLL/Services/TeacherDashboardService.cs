using AutoMapper;
using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.Dashboard;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class TeacherDashboardService : ITeacherDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TeacherDashboardService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<TeacherDashboardDto>> GetTeacherDashboardAsync(int teacherId)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
        if (teacher is null || teacher.IsDeleted)
            return OperationResult<TeacherDashboardDto>.Failure("المعلم غير موجود", 404);
        if (teacher.Role != UserRole.Teacher)
            return OperationResult<TeacherDashboardDto>.Failure("المستخدم ليس معلماً", 400);

        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear is null)
            return OperationResult<TeacherDashboardDto>.Failure("لا توجد سنة دراسية نشطة", 404);

        var dto = new TeacherDashboardDto
        {
            UserName = teacher.FullName
        };

        // 1. Teacher's class-subject assignments
        var assignments = await _unitOfWork.ClassSubjectTeachers
            .GetByTeacherWithAllDetailsAsync(teacherId, currentYear.Id);

        // 2. Today's classes (Sunday=0, Monday=1, ..., Thursday=4)
        //    DayOfWeek in C#: Sunday=0, Monday=1, ..., Saturday=6
        var todayCsharpDay = (int)DateTime.UtcNow.DayOfWeek; // 0-6
        var slots = await _unitOfWork.TimetableSlots.GetTeacherScheduleAsync(teacherId, currentYear.Id);
        dto.TodayClassesCount = slots.Count(s => (int)s.DayOfWeek == todayCsharpDay && !s.IsBreak);

        // 3. Student counts per class and total
        var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
        int totalStudents = 0;

        foreach (var classId in classIds)
        {
            var count = await _unitOfWork.StudentEnrollments
                .GetActiveCountByClassAsync(classId, currentYear.Id);
            totalStudents += count;
        }

        dto.TotalStudentsCount = totalStudents;

        // 4. Build classes list with student counts
        var classStudentCounts = new Dictionary<int, int>();
        foreach (var classId in classIds)
        {
            var count = await _unitOfWork.StudentEnrollments
                .GetActiveCountByClassAsync(classId, currentYear.Id);
            classStudentCounts[classId] = count;
        }

        var colors = new[] { "#4F46E5", "#10B981", "#D97706", "#059669", "#6366F1" };
        int colorIdx = 0;

        foreach (var a in assignments)
        {
            dto.Classes.Add(new TeacherClassDto
            {
                ClassId = a.ClassId,
                ClassName = a.Class?.Name ?? $"الفصل {a.ClassId}",
                SubjectId = a.SubjectId,
                SubjectName = a.Subject?.Name ?? "",
                ClassSubjectTeacherId = a.Id,
                StudentCount = classStudentCounts.GetValueOrDefault(a.ClassId, 0)
            });
            colorIdx++;
        }

        // 5. Pending submissions (ungraded)
        var pendingSubmissions = await _unitOfWork.StudentAssignmentSubmissions
            .GetPendingByTeacherAsync(teacherId, currentYear.Id);

        dto.PendingSubmissionsCount = pendingSubmissions.Count;

        // 6. Tasks
        var taskTitles = pendingSubmissions
            .GroupBy(s => s.AssignmentId)
            .Select(g =>
            {
                var first = g.First();
                return $"تصحيح واجب: {first.Assignment?.Title ?? "—"} ({g.Count()} تسليمات بانتظار التقييم)";
            })
            .ToList();

        dto.Tasks = taskTitles.Count > 0
            ? taskTitles
            : new List<string>
            {
                "لا يوجد واجبات بانتظار التصحيح حالياً",
                "تحضير الحصص القادمة"
            };

        return OperationResult<TeacherDashboardDto>.Success(dto, "تم تحميل بيانات لوحة التحكم بنجاح");
    }
}
