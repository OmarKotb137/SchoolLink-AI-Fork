using Microsoft.EntityFrameworkCore;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;
using Project.DAL.Context;

namespace Project.BLL.AI.ExamAgent.Infrastructure;

public class DbLessonRepository : ILessonRepository
{
    private readonly AppDbContext _db;

    public DbLessonRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Lesson>> SearchAsync(string? subject)
    {
        var query = _db.Lessons
            .Include(l => l.Unit)
            .ThenInclude(u => u.Subject)
            .Where(l => !l.IsDeleted);

        if (!string.IsNullOrWhiteSpace(subject))
            query = query.Where(l =>
                l.Unit.Subject.Name.Contains(subject) ||
                l.Title.Contains(subject));

        var lessons = await query.OrderBy(l => l.DisplayOrder).ToListAsync();

        return lessons.Select(l => new Lesson
        {
            Id = l.Id,
            Title = l.Title,
            Subject = l.Unit.Subject.Name,
            Content = l.Content
        }).ToList();
    }

    public async Task<Lesson?> GetByIdAsync(int id)
    {
        var entity = await _db.Lessons
            .Include(l => l.Unit)
            .ThenInclude(u => u.Subject)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (entity == null) return null;

        return new Lesson
        {
            Id = entity.Id,
            Title = entity.Title,
            Subject = entity.Unit.Subject.Name,
            Content = entity.Content
        };
    }

    public async Task<bool> UpdateAsync(int id, string title, string content)
    {
        var entity = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        if (entity == null) return false;

        entity.Title = title;
        entity.Content = content;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
