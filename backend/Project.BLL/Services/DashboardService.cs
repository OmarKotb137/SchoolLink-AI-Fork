using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Dashboard;
using Project.BLL.DTOs.Users;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IClassService _classService;
    private readonly IFinalGradeService _finalGradeService;
    private readonly IMapper _mapper;

    public DashboardService(
        IUnitOfWork unitOfWork,
        IUserService userService,
        IClassService classService,
        IFinalGradeService finalGradeService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _classService = classService;
        _finalGradeService = finalGradeService;
        _mapper = mapper;
    }

    public async Task<OperationResult<AdminDashboardDto>> GetAdminDashboardAsync(int? academicYearId = null)
    {
        var dto = new AdminDashboardDto();

        // Total Students & Teachers
        var userStatsResult = await _userService.GetUserStatsAsync();
        if (userStatsResult.IsSuccess && userStatsResult.Data is not null)
        {
            dto.TotalStudents = userStatsResult.Data.Students;
            dto.TotalTeachers = userStatsResult.Data.Teachers;
        }

        // Total Classes
        var classCountResult = await _classService.GetClassCountAsync(academicYearId);
        if (classCountResult.IsSuccess)
            dto.TotalClasses = classCountResult.Data;

        // Success Rate
        dto.SuccessRate = await CalculateSuccessRateAsync(academicYearId);

        // Weekly Activity (last 7 days by default)
        dto.WeeklyActivity = await GetWeeklyActivityAsync();

        // Recent Activities
        dto.RecentActivities = await GetRecentActivitiesAsync();

        // Recent Users
        dto.RecentUsers = await GetRecentUsersAsync();

        return OperationResult<AdminDashboardDto>.Success(dto, "تم جلب بيانات لوحة التحكم بنجاح");
    }

    private async Task<decimal> CalculateSuccessRateAsync(int? academicYearId)
    {
        try
        {
            IReadOnlyList<SchoolClass> classes;
            if (academicYearId.HasValue)
                classes = await _unitOfWork.Classes.FindAsync(c =>
                    c.AcademicYearId == academicYearId.Value && !c.IsDeleted);
            else
                classes = await _unitOfWork.Classes.FindAsync(c => !c.IsDeleted);

            if (!classes.Any())
                return 0;

            var totalStudents = 0;
            var passedStudents = 0;

            foreach (var cls in classes)
            {
                var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(cls.Id);
                foreach (var grade in grades.Where(g => !g.IsDeleted))
                {
                    totalStudents++;
                    if (grade.Total >= 50)
                        passedStudents++;
                }
            }

            return totalStudents > 0
                ? Math.Round((decimal)passedStudents / totalStudents * 100, 1)
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<List<WeeklyActivityDto>> GetWeeklyActivityAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var absences = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.AbsenceDate >= weekStart && a.AbsenceDate <= today && a.IsAbsent && !a.IsDeleted);

            var dayNames = new[] { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };
            var result = new List<WeeklyActivityDto>();

            for (int i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i);
                var count = absences.Count(a => a.AbsenceDate == date);
                result.Add(new WeeklyActivityDto
                {
                    Day = dayNames[i],
                    Count = count
                });
            }

            return result;
        }
        catch
        {
            return new List<WeeklyActivityDto>();
        }
    }

    private async Task<List<string>> GetRecentActivitiesAsync()
    {
        try
        {
            var activities = new List<string>();

            var recentUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var recentUsersList = recentUsers
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var user in recentUsersList)
                activities.Add($"تسجيل {GetRoleNameArabic(user.Role)} جديد - {user.FullName}");

            var recentClasses = await _unitOfWork.Classes.FindAsync(c => !c.IsDeleted);
            var recentClassesList = recentClasses
                .OrderByDescending(c => c.CreatedAt)
                .Take(3)
                .ToList();

            foreach (var cls in recentClassesList)
                activities.Add($"إضافة فصل جديد - {cls.Name}");

            return activities;
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task<List<RecentUserDto>> GetRecentUsersAsync()
    {
        try
        {
            var users = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted && u.IsActive);
            var recentUsers = users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToList();

            return recentUsers.Select(u => new RecentUserDto
            {
                Name = u.FullName,
                Role = GetRoleNameArabic(u.Role),
                Email = u.ContactEmail,
                Status = u.IsActive ? "نشط" : "غير نشط"
            }).ToList();
        }
        catch
        {
            return new List<RecentUserDto>();
        }
    }

    private static string GetRoleNameArabic(UserRole role) => role switch
    {
        UserRole.Admin => "مدير",
        UserRole.Teacher => "معلم",
        UserRole.Student => "طالب",
        UserRole.Parent => "ولي أمر",
        _ => role.ToString()
    };
}
