namespace Project.BLL.DTOs.DailyAbsences;

public class AbsenceSummaryDto
{
    public int TotalAbsences { get; set; }
    public IEnumerable<SubjectAbsenceBreakdown> PerSubjectBreakdown { get; set; } = new List<SubjectAbsenceBreakdown>();
    public IEnumerable<DateOnly> AbsenceDates { get; set; } = new List<DateOnly>();
}

public class SubjectAbsenceBreakdown
{
    public string SubjectName { get; set; } = string.Empty;
    public int AbsenceCount { get; set; }
}
