using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Project.BLL.AI.Agents;
using Project.BLL.AI.ExamAgent.Infrastructure;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Services;
using Project.BLL.AI.ExamAgent.Tools;
using Project.BLL.AI.Infrastructure;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Services;
using Project.BLL.AI.Providers;
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
        services.AddScoped<IExamHtmlRenderer, ExamHtmlRenderer>();
        services.AddScoped<IExamMediaService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(IExamMediaService));
            var logger = sp.GetRequiredService<ILogger<ExamMediaService>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IWebHostEnvironment>();

            var apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
            var mediaFolder = Path.Combine(env.WebRootPath, "exam-media");

            return new ExamMediaService(httpClient, logger, apiKey, mediaFolder);
        });
        services.AddScoped<IAIGenerationLogService, AIGenerationLogService>();
        services.AddScoped<IEvaluationTemplateService, EvaluationTemplateService>();
        services.AddScoped<IEvaluationPeriodService, EvaluationPeriodService>();
        services.AddScoped<IEvaluationItemService, EvaluationItemService>();
        services.AddScoped<IStudentEvaluationService, StudentEvaluationService>();
        services.AddScoped<IDailyAbsenceService, DailyAbsenceService>();
        services.AddScoped<IPeriodAverageService, PeriodAverageService>();
        services.AddScoped<IPeriodicAssessmentService, PeriodicAssessmentService>();
        services.AddScoped<IFinalGradeService, FinalGradeService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
        services.AddScoped<IParentStudentService, ParentStudentService>();
        services.AddScoped<IStudyPlanService, StudyPlanService>();
        services.AddScoped<ILessonFeedbackService, LessonFeedbackService>();
        services.AddScoped<IUnitService, UnitService>();

        // AI Services
        RegisterAiServices(services, config);
        RegisterExamAgentServices(services, config);

        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

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

    private static void RegisterAiServices(IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient("Gemini", c => c.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient("DeepSeek", c => c.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient("MistralOcr", c =>
        {
            c.Timeout = TimeSpan.FromMinutes(5); // OCR على ملفات كبيرة قد يحتاج وقتاً أطول
        });

        services.AddScoped<GeminiProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient("Gemini");
            var logger = sp.GetRequiredService<ILogger<GeminiProvider>>();
            var apiKey = config["AI:Gemini:ApiKey"] ?? "";
            var model = config["AI:Gemini:Model"] ?? "gemini-2.0-flash";
            return new GeminiProvider(http, logger, apiKey, model);
        });

        services.AddScoped<DeepSeekProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient("DeepSeek");
            var logger = sp.GetRequiredService<ILogger<DeepSeekProvider>>();
            var apiKey = config["AI:DeepSeek:ApiKey"] ?? "";
            var model = config["AI:DeepSeek:Model"] ?? "deepseek-chat";
            return new DeepSeekProvider(http, logger, apiKey, model);
        });

        services.AddScoped<OpenRouterProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient();
            var logger = sp.GetRequiredService<ILogger<OpenRouterProvider>>();
            var apiKey = config["LlmSettings:OpenRouter:ApiKey"] ?? "";
            var model = config["LlmSettings:OpenRouter:LessonCorrectionModel"] ?? "openrouter/owl-alpha";
            return new OpenRouterProvider(http, logger, apiKey, model);
        });

        services.AddScoped<ILLMProvider>(sp => sp.GetRequiredService<GeminiProvider>());
        services.AddScoped<ILLMProvider>(sp => sp.GetRequiredService<DeepSeekProvider>());
        services.AddScoped<ILLMProvider>(sp => sp.GetRequiredService<OpenRouterProvider>());

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<ILLMRouter, LLMRouter>();

        services.AddScoped<IStudentAssistantAgent, StudentAssistantAgent>();
        services.AddScoped<ITeacherAssistantAgent, TeacherAssistantAgent>();
        services.AddScoped<IParentAssistantAgent, ParentAssistantAgent>();

        services.AddScoped<IExamGeneratorService, ExamGeneratorService>();
        services.AddScoped<IEvaluationReportService, EvaluationReportService>();
        services.AddScoped<IStudyScheduleOptimizerService, StudyScheduleOptimizerService>();
        services.AddScoped<IStudentImportService, StudentImportService>();
        services.AddScoped<IBookParserService, BookParserService>();
    }

    private static void RegisterExamAgentServices(IServiceCollection services, IConfiguration config)
    {
        services.RegisterLlmClient(config);

        services.AddScoped<BLL.AI.ExamAgent.Interfaces.ILessonRepository, DbLessonRepository>();
        services.AddScoped<IExamGenerator, LlmExamGenerator>();

        services.AddScoped<IAgentTool, GetLessonsTool>();
        services.AddScoped<IAgentTool, GetLessonContentTool>();
        services.AddScoped<IAgentTool, GenerateExamTool>();

        services.AddScoped<IAgentChatStore, AgentChatStore>();
        services.AddScoped<AgentToolRegistry>();
        services.AddScoped<ExamAgentService>();
    }
}
