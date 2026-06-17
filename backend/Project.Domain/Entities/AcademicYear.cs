namespace Project.Domain.Entities
{
    public class AcademicYear : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsCurrent { get; set; } = false;

        // Semester support (all manual — no auto-calc)
        public DateOnly? FirstSemesterStartDate { get; set; }
        public DateOnly? FirstSemesterEndDate { get; set; }
        public DateOnly? SecondSemesterStartDate { get; set; }
        public DateOnly? SecondSemesterEndDate { get; set; }
    }
}
