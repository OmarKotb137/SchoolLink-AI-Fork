using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Project.BLL.Interfaces;
using Project.BLL.Mapping;
using Project.BLL.Services;
using Project.BLL.Validators;
using Project.BLL.DTOs;
using Project.DAL.Context;
using Project.DAL.Interfaces;
using Project.DAL.UnitOfWork;

namespace Project.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserService, UserService>();

        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

        return services;
    }
}
