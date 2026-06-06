using Project.BLL.AI.ExamAgent.Models;

namespace Project.BLL.AI.ExamAgent.Interfaces;

public interface ILessonRepository
{
    Task<List<Lesson>> SearchAsync(string? subject);
    Task<Lesson?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, string title, string content);
}

public interface IExamGenerator
{
    Task<ExamResponse> GenerateAsync(ExamRequest request);
}
