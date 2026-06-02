using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Project.BLL.Interfaces;
using Project.BLL.Mapping;
using Project.BLL.Services;
using Project.BLL.Validators;
using Project.DAL.Context;
using Project.DAL.Interfaces;
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

        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
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
