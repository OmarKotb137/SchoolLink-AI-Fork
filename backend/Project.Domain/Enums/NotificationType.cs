namespace Project.Domain.Enums
{
    public enum NotificationType
    {
        // === الموجودة حالياً (1-9) ===
        GradeAlert = 1,
        BehaviorAlert = 2,
        AbsenceAlert = 3,
        NewAssignment = 4,          // مؤجل - Training
        ExamReminder = 5,
        MonthlyReport = 6,
        GradePublished = 7,
        SystemAlert = 8,
        ImprovementAlert = 9,

        // === Academic (10-28) ===
        PositiveBehavior = 10,
        DisciplinaryAction = 11,
        TopStudent = 16,
        GradeThresholdAlert = 26,
        AcademicProbation = 27,
        ExcessiveAbsenceWarning = 28,

        // === عام (17-29) ===
        Announcement = 17,
        SchoolEvent = 18,
        Holiday = 19,
        EmergencyAlert = 20,
        ScheduleChanged = 21,
        SubstituteTeacher = 22,
        NewMessage = 23,
        GroupChatInvite = 24,
        ParentMeetingRequest = 29,

        // === مؤجل - Training (4, 12-15, 32-34) ===
        HomeworkSubmitted = 12,
        HomeworkGraded = 13,
        Exam = 14,
        ExamResult = 15,
        ExamScheduleChanged = 32,
        ExamSchedulePublished = 33,
        ExamCheatingAlert = 34
    }
}
