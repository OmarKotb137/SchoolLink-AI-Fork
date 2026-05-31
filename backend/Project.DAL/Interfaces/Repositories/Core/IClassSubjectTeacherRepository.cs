using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface IClassSubjectTeacherRepository : IRepository<ClassSubjectTeacher>
{
    Task<ClassSubjectTeacher?>               GetByClassSubjectAndYearAsync(int classId, int subjectId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassSubjectTeacher>> GetByTeacherAndYearAsync(int teacherId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassSubjectTeacher>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassSubjectTeacher>> GetBySubjectAndYearAsync(int subjectId, int academicYearId, CancellationToken ct = default);

    Task<bool> IsTeacherAssignedAsync(int teacherId, int classId, int subjectId, int academicYearId, CancellationToken ct = default);
    Task<bool> ExistsByClassSubjectAndYearAsync(int classId, int subjectId, int academicYearId, CancellationToken ct = default);

    Task<IReadOnlyList<SchoolClass>> GetClassesForTeacherAsync(int teacherId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<Subject>>     GetSubjectsForTeacherAsync(int teacherId, int academicYearId, CancellationToken ct = default);

    Task<ClassSubjectTeacher?>               GetWithDetailsAsync(int classSubjectTeacherId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassSubjectTeacher>> GetByClassWithTeacherAsync(int classId, int academicYearId, CancellationToken ct = default);
}



