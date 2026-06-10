using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IExamGenerator
{
    Task<ExamResponse> GenerateAsync(ExamRequest request);
}
