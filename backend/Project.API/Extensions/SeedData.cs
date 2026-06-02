using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.API.Extensions;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await context.Users.AnyAsync())
            return;

        var admin = new User
        {
            FullName = "Super Admin",
            Email = "admin@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
