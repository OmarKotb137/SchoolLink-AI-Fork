using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class ClassSubjectTeacherRepository : Repository<ClassSubjectTeacher>, IClassSubjectTeacherRepository
{
    public ClassSubjectTeacherRepository(AppDbContext context) : base(context) { }


    public async Task<ClassSubjectTeacher?> GetByClassSubjectAndYearAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Include(cst => cst.Teacher)
            .Include(cst => cst.Subject)
            .FirstOrDefaultAsync(cst =>
                cst.ClassId == classId &&
                cst.SubjectId == subjectId &&
                cst.AcademicYearId == academicYearId, ct);

    public async Task<IReadOnlyList<ClassSubjectTeacher>> GetByTeacherAndYearAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Where(cst =>
                cst.TeacherId == teacherId &&
                cst.AcademicYearId == academicYearId)
            .Include(cst => cst.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(cst => cst.Subject)
            .OrderBy(cst => cst.Class.GradeLevel.LevelOrder)
            .ThenBy(cst => cst.Class.Name)
            .ThenBy(cst => cst.Subject.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClassSubjectTeacher>> GetByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Where(cst =>
                cst.ClassId == classId &&
                cst.AcademicYearId == academicYearId)
            .Include(cst => cst.Teacher)
            .Include(cst => cst.Subject)
            .OrderBy(cst => cst.Subject.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClassSubjectTeacher>> GetBySubjectAndYearAsync(
        int subjectId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Where(cst =>
                cst.SubjectId == subjectId &&
                cst.AcademicYearId == academicYearId)
            .Include(cst => cst.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(cst => cst.Teacher)
            .OrderBy(cst => cst.Class.GradeLevel.LevelOrder)
            .ThenBy(cst => cst.Class.Name)
            .ToListAsync(ct);


    public async Task<bool> IsTeacherAssignedAsync(
        int teacherId,
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .AnyAsync(cst =>
                cst.TeacherId == teacherId &&
                cst.ClassId == classId &&
                cst.SubjectId == subjectId &&
                cst.AcademicYearId == academicYearId, ct);

    public async Task<bool> ExistsByClassSubjectAndYearAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .AnyAsync(cst =>
                cst.ClassId == classId &&
                cst.SubjectId == subjectId &&
                cst.AcademicYearId == academicYearId, ct);


    public async Task<IReadOnlyList<SchoolClass>> GetClassesForTeacherAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Where(cst =>
                cst.TeacherId == teacherId &&
                cst.AcademicYearId == academicYearId)
            .Select(cst => cst.Class)
            .Distinct()
            .Include(c => c.GradeLevel)
            .OrderBy(c => c.GradeLevel.LevelOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Subject>> GetSubjectsForTeacherAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Where(cst =>
                cst.TeacherId == teacherId &&
                cst.AcademicYearId == academicYearId)
            .Select(cst => cst.Subject)
            .Distinct()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);


    public async Task<ClassSubjectTeacher?> GetWithDetailsAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Include(cst => cst.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(cst => cst.Subject)
            .Include(cst => cst.Teacher)
            .FirstOrDefaultAsync(cst => cst.Id == classSubjectTeacherId, ct);

    public async Task<IReadOnlyList<ClassSubjectTeacher>> GetByClassWithTeacherAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.ClassSubjectTeachers
            .Where(cst =>
                cst.ClassId == classId &&
                cst.AcademicYearId == academicYearId)
            .Include(cst => cst.Teacher)
            .Include(cst => cst.Subject)
            .OrderBy(cst => cst.Subject.Name)
            .ToListAsync(ct);
}



