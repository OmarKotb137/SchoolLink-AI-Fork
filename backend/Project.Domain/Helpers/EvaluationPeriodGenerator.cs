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

    /// <summary>Generate weekly + monthly periods for a full year (no semesters — backward compatible).</summary>
    public static IReadOnlyList<EvaluationPeriod> GeneratePeriods(int academicYearId, DateOnly yearStart, DateOnly yearEnd)
    {
        return GeneratePeriodsCore(academicYearId, yearStart, yearEnd, null, null, null, null);
    }

    /// <summary>Generate periods for a single semester only.</summary>
    public static IReadOnlyList<EvaluationPeriod> GeneratePeriodsForSemester(
        int academicYearId, DateOnly semStart, DateOnly semEnd, int semesterNumber)
    {
        var now = DateTime.UtcNow;
        var periods = new List<EvaluationPeriod>();

        var weeks = AddWeeksForSemester(periods, academicYearId, semStart, semEnd, semesterNumber, now);
        periods.AddRange(BuildMonthlyPeriods(academicYearId, weeks, now));

        return periods;
    }

    /// <summary>Generate periods split across two semesters with semester-level grouping.</summary>
    public static IReadOnlyList<EvaluationPeriod> GeneratePeriods(
        int academicYearId,
        DateOnly yearStart,
        DateOnly? sem1Start,
        DateOnly? sem1End,
        DateOnly? sem2Start,
        DateOnly? sem2End)
    {
        // If no semester dates, fall back to single-year generation
        if (sem1Start is null || sem1End is null || sem2Start is null || sem2End is null)
            return GeneratePeriods(academicYearId, yearStart, sem2End ?? sem1End ?? yearStart);

        return GeneratePeriodsCore(academicYearId, yearStart, yearStart, sem1Start, sem1End, sem2Start, sem2End);
    }

    private static IReadOnlyList<EvaluationPeriod> GeneratePeriodsCore(
        int academicYearId,
        DateOnly yearStart,
        DateOnly yearEnd,
        DateOnly? sem1Start,
        DateOnly? sem1End,
        DateOnly? sem2Start,
        DateOnly? sem2End)
    {
        var now = DateTime.UtcNow;
        var periods = new List<EvaluationPeriod>();

        // --- Single-year mode (no semesters) ---
        if (sem1Start is null || sem1End is null)
        {
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

            // Monthly periods
            periods.AddRange(BuildMonthlyPeriods(academicYearId, periods, now));
            return periods;
        }

        // --- Semester mode ---
        // Generate Semester period type entries
        var sem1Weeks = AddWeeksForSemester(periods, academicYearId, sem1Start.Value, sem1End.Value, 1, now);
        var sem2Weeks = AddWeeksForSemester(periods, academicYearId, sem2Start.Value, sem2End.Value, 2, now, weekOffset: sem1Weeks.Count);

        // Add Semester-type periods
        periods.Add(new EvaluationPeriod
        {
            AcademicYearId = academicYearId,
            Name = "الترم الأول",
            PeriodType = PeriodType.Semester,
            SemesterNumber = 1,
            OrderNum = 1,
            StartDate = sem1Start,
            EndDate = sem1End,
            CreatedAt = now,
            UpdatedAt = now
        });

        periods.Add(new EvaluationPeriod
        {
            AcademicYearId = academicYearId,
            Name = "الترم الثاني",
            PeriodType = PeriodType.Semester,
            SemesterNumber = 2,
            OrderNum = 2,
            StartDate = sem2Start,
            EndDate = sem2End,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Monthly periods per semester
        periods.AddRange(BuildMonthlyPeriods(academicYearId, sem1Weeks, now));
        periods.AddRange(BuildMonthlyPeriods(academicYearId, sem2Weeks, now));

        return periods;
    }

    private static List<EvaluationPeriod> AddWeeksForSemester(
        List<EvaluationPeriod> targetList,
        int academicYearId,
        DateOnly semStart,
        DateOnly semEnd,
        int semesterNumber,
        DateTime now,
        int weekOffset = 0)
    {
        var weeks = new List<EvaluationPeriod>();
        var totalDays = semEnd.DayNumber - semStart.DayNumber;
        var totalWeeks = Math.Max(1, (totalDays + 6) / 7);

        for (int w = 0; w < totalWeeks; w++)
        {
            var weekStart = semStart.AddDays(w * 7);
            var weekEnd = weekStart.AddDays(6);
            if (weekEnd > semEnd) weekEnd = semEnd;

            var period = new EvaluationPeriod
            {
                AcademicYearId = academicYearId,
                Name = $"الأسبوع {w + 1}",
                PeriodType = PeriodType.Weekly,
                SemesterNumber = semesterNumber,
                OrderNum = w + 1,
                StartDate = weekStart,
                EndDate = weekEnd,
                MonthName = ArabicMonths[weekStart.Month - 1],
                CreatedAt = now,
                UpdatedAt = now
            };

            weeks.Add(period);
            targetList.Add(period);
        }

        return weeks;
    }

    private static List<EvaluationPeriod> BuildMonthlyPeriods(
        int academicYearId,
        List<EvaluationPeriod> sourceWeeks,
        DateTime now)
    {
        var semesterNumber = sourceWeeks.FirstOrDefault()?.SemesterNumber;

        return sourceWeeks
            .GroupBy(p => p.MonthName)
            .Select((g, i) => new EvaluationPeriod
            {
                AcademicYearId = academicYearId,
                Name = $"شهر {g.Key}",
                PeriodType = PeriodType.Monthly,
                SemesterNumber = semesterNumber,
                OrderNum = i + 1,
                StartDate = g.Min(p => p.StartDate),
                EndDate = g.Max(p => p.EndDate),
                MonthName = g.Key,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();
    }
}
