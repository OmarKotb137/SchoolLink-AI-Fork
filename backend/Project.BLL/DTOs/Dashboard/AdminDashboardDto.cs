namespace Project.BLL.DTOs.Dashboard;

public class AdminDashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalClasses { get; set; }
    public decimal SuccessRate { get; set; }
    public List<WeeklyActivityDto> WeeklyActivity { get; set; } = new();
    public List<string> RecentActivities { get; set; } = new();
    public List<RecentUserDto> RecentUsers { get; set; } = new();
}

public class WeeklyActivityDto
{
    public string Day { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
