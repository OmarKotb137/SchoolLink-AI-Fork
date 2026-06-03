using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Project.BLL.Interfaces;
using Project.BLL.Mapping;
using Project.BLL.Services;
using Project.BLL.Validators;
using Project.DAL.Context;
using Project.DAL.Interfaces;
using Project.DAL.Interfaces.Repositories;
using Project.DAL.Repositories;
using Project.DAL.UnitOfWork;

namespace Project.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        RegisterRepositories(services);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IResultVisibilityService, ResultVisibilityService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<ISchoolProfileService, SchoolProfileService>();
        services.AddHttpClient<IDropboxService, DropboxService>();


        services.AddScoped<IAcademicYearService, AcademicYearService>();
        services.AddScoped<IGradeLevelService, GradeLevelService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IClassSubjectTeacherService, ClassSubjectTeacherService>();
        services.AddScoped<ITimetableService, TimetableService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IAssignmentSubmissionService, AssignmentSubmissionService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IExamAttemptService, ExamAttemptService>();
        services.AddScoped<IAIGenerationLogService, AIGenerationLogService>();
        services.AddScoped<IEvaluationTemplateService, EvaluationTemplateService>();
        services.AddScoped<IEvaluationPeriodService, EvaluationPeriodService>();
        services.AddScoped<IEvaluationItemService, EvaluationItemService>();
        services.AddScoped<IStudentEvaluationService, StudentEvaluationService>();
        services.AddScoped<IDailyAbsenceService, DailyAbsenceService>();
        services.AddScoped<IPeriodAverageService, PeriodAverageService>();
        services.AddScoped<IPeriodicAssessmentService, PeriodicAssessmentService>();
        services.AddScoped<IFinalGradeService, FinalGradeService>();

        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        var dalAssembly = typeof(Repository<>).Assembly;
        var repositoryTypes = dalAssembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.Name.EndsWith("Repository", StringComparison.Ordinal) &&
                !type.IsGenericTypeDefinition);

        foreach (var repositoryType in repositoryTypes)
        {
            var repositoryInterface = repositoryType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{repositoryType.Name}");

            if (repositoryInterface is not null)
                services.AddScoped(repositoryInterface, repositoryType);
        }
    }
}
