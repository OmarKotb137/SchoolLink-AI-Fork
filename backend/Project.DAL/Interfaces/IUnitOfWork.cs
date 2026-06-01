using Project.DAL.Interfaces.Repositories.Core;
using Project.DAL.Interfaces.Repositories.Evaluation;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.DAL.Interfaces.Repositories.Communication;
using Project.DAL.Interfaces.Repositories.Library;
using Project.DAL.Interfaces.Repositories.Feedback;
using Project.DAL.Interfaces.Repositories.Settings;
using Project.DAL.Interfaces.Repositories.Timetable;
using Project.DAL.Interfaces.Repositories.StudyPlans;

namespace Project.DAL.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // Section A: Core
    IUserRepository                        Users                        { get; }
    IAcademicYearRepository                AcademicYears                { get; }
    IGradeLevelRepository                  GradeLevels                  { get; }
    ISubjectRepository                     Subjects                     { get; }
    ISchoolClassRepository                 Classes                      { get; }
    IStudentRepository                     Students                     { get; }
    IStudentEnrollmentRepository           StudentEnrollments           { get; }
    IParentStudentRepository               ParentStudents               { get; }
    IClassSubjectTeacherRepository         ClassSubjectTeachers         { get; }

    // Section B: Evaluation (Academic)
    IEvaluationTemplateRepository          EvaluationTemplates          { get; }
    IEvaluationItemRepository              EvaluationItems              { get; }
    IEvaluationPeriodRepository            EvaluationPeriods            { get; }
    IStudentEvaluationRepository           StudentEvaluations           { get; }
    IDailyAbsenceRepository                DailyAbsences                { get; }
    IPeriodAverageRepository               PeriodAverages               { get; }
    IPeriodicAssessmentRepository          PeriodicAssessments          { get; }
    IFinalGradeRepository                  FinalGrades                  { get; }

    // Section C: Learning (Training)
    IAssignmentRepository                  Assignments                  { get; }
    IAssignmentQuestionRepository          AssignmentQuestions          { get; }
    IAssignmentQuestionOptionRepository    AssignmentQuestionOptions    { get; }
    IStudentAssignmentSubmissionRepository StudentAssignmentSubmissions { get; }
    IStudentAssignmentAnswerRepository     StudentAssignmentAnswers     { get; }
    IExamRepository                        Exams                        { get; }
    IExamQuestionRepository                ExamQuestions                { get; }
    IExamQuestionOptionRepository          ExamQuestionOptions          { get; }
    IStudentExamAttemptRepository          StudentExamAttempts          { get; }
    IStudentExamAnswerRepository           StudentExamAnswers           { get; }

    // Section D: Communication
    IConversationRepository                Conversations                { get; }
    IConversationParticipantRepository     ConversationParticipants     { get; }
    IMessageRepository                     Messages                     { get; }
    INotificationRepository                Notifications                { get; }
    IAnnouncementRepository                Announcements                { get; }

    // Section E: Library
    ILibraryItemRepository                 LibraryItems                 { get; }

    // Section F: Feedback
    ILessonFeedbackRepository              LessonFeedbacks              { get; }

    // Section G: Settings
    IResultVisibilitySettingRepository     ResultVisibilitySettings     { get; }
    IAIGenerationLogRepository             AIGenerationLogs             { get; }
    ISchoolProfileRepository               SchoolProfiles               { get; }

    // Section H: Timetable
    ITimetableRepository                   Timetables                   { get; }
    ITimetableSlotRepository               TimetableSlots               { get; }

    // Section I: Study Plans
    IStudyPlanRepository                   StudyPlans                   { get; }
    IStudyPlanItemRepository               StudyPlanItems               { get; }

    // Persistence

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // Transaction Management

    Task BeginTransactionAsync(CancellationToken ct = default);

    Task CommitTransactionAsync(CancellationToken ct = default);

    Task RollbackTransactionAsync(CancellationToken ct = default);

    Task    ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}

