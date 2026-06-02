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

    /// <summary>
    /// يجيب ClassSubjectTeacher بـ Class + Subject + Teacher + AcademicYear كلهم محملين.
    /// يُستخدم في AssignTeacherAsync بعد الـ save لبناء الـ DTO الكامل.
    /// (GetWithDetailsAsync الموجودة مش بتجيب AcademicYear)
    /// </summary>
    Task<ClassSubjectTeacher?> GetWithAllDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// يجيب كل assignments لـ class معينة مع Class + Subject + Teacher + AcademicYear محملين.
    /// يُستخدم في GetByClassAsync.
    /// (GetByClassWithTeacherAsync الموجودة مش بتجيب Class و AcademicYear)
    /// </summary>
    Task<IReadOnlyList<ClassSubjectTeacher>> GetByClassWithAllDetailsAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default);

    /// <summary>
    /// يجيب كل assignments لـ teacher معين مع Class + Subject + Teacher + AcademicYear محملين.
    /// يُستخدم في GetByTeacherAsync.
    /// (GetByTeacherAndYearAsync الموجودة مش بتجيب Teacher و AcademicYear)
    /// </summary>
    Task<IReadOnlyList<ClassSubjectTeacher>> GetByTeacherWithAllDetailsAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default);
}



