using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IEvaluationTemplateRepository : IRepository<EvaluationTemplate>
{
    Task<EvaluationTemplate?> GetByGradeLevelSubjectAndYearAsync(int gradeLevelId, int subjectId, int academicYearId, CancellationToken ct = default);

    Task<IReadOnlyList<EvaluationTemplate>> GetByGradeLevelAndYearAsync(int gradeLevelId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<EvaluationTemplate>> GetByAcademicYearAsync(int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<EvaluationTemplate>> GetActiveAsync(CancellationToken ct = default);

    Task<EvaluationTemplate?> GetWithItemsAsync(int templateId, CancellationToken ct = default);

    Task<bool> ExistsByGradeLevelSubjectAndYearAsync(int gradeLevelId, int subjectId, int academicYearId, CancellationToken ct = default);
}



