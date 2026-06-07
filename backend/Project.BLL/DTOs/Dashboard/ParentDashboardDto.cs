namespace Project.BLL.DTOs.Dashboard;

public class ParentDashboardDto
{
    public List<ParentChildDto> Children { get; set; } = new();
    public List<string> RecentActivities { get; set; } = new();
}

public class ParentChildDto
{
    public string Name { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public decimal Performance { get; set; }
    public ChildGradesDto Grades { get; set; } = new();
    public int Absences { get; set; }
}

public class ChildGradesDto
{
    public string Last { get; set; } = "—";
    public string Total { get; set; } = "—";
}
