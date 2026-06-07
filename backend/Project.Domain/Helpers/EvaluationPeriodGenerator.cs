using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.Domain.Helpers;

public static class EvaluationPeriodGenerator
{
    private static readonly string[] ArabicMonths =
    {
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    };

    public static IReadOnlyList<EvaluationPeriod> GeneratePeriods(int academicYearId, DateOnly yearStart, DateOnly yearEnd)
    {
        var now = DateTime.UtcNow;
        var periods = new List<EvaluationPeriod>();
        var totalDays = yearEnd.DayNumber - yearStart.DayNumber;
        var totalWeeks = Math.Max(1, (totalDays + 6) / 7);

        for (int w = 0; w < totalWeeks; w++)
        {
            var weekStart = yearStart.AddDays(w * 7);
            var weekEnd = weekStart.AddDays(6);
            if (weekEnd > yearEnd) weekEnd = yearEnd;

            periods.Add(new EvaluationPeriod
            {
                AcademicYearId = academicYearId,
                Name = $"الأسبوع {w + 1}",
                PeriodType = PeriodType.Weekly,
                OrderNum = w + 1,
                StartDate = weekStart,
                EndDate = weekEnd,
                MonthName = ArabicMonths[weekStart.Month - 1],
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        var monthlyGroups = periods
            .GroupBy(p => p.MonthName)
            .Select((g, i) => new EvaluationPeriod
            {
                AcademicYearId = academicYearId,
                Name = $"شهر {g.Key}",
                PeriodType = PeriodType.Monthly,
                OrderNum = i + 1,
                StartDate = g.Min(p => p.StartDate),
                EndDate = g.Max(p => p.EndDate),
                MonthName = g.Key,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        periods.AddRange(monthlyGroups);
        return periods;
    }
}
