using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface ILessonRepository
{
    Task<List<Lesson>> SearchAsync(string? subject);
    Task<Lesson?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, string title, string content);
}
