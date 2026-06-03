using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ClassesController(IUnitOfWork uow) { _uow = uow; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var classes = await _uow.Classes.GetFilteredWithIncludesAsync(null, null);
        var result = new List<object>();
        foreach (var c in classes)
        {
            var enrollments = await _uow.StudentEnrollments.GetByClassWithStudentAsync(c.Id, c.AcademicYearId);
            var cst = (await _uow.ClassSubjectTeachers.GetByClassWithAllDetailsAsync(c.Id, c.AcademicYearId)).FirstOrDefault();
            result.Add(MapClass(c, enrollments, cst));
        }
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _uow.Classes.GetByIdWithIncludesAsync(id);
        if (c is null || c.IsDeleted)
            return NotFound(new { isSuccess = false, message = "الفصل غير موجود" });
        var enrollments = await _uow.StudentEnrollments.GetByClassWithStudentAsync(c.Id, c.AcademicYearId);
        var cst = (await _uow.ClassSubjectTeachers.GetByClassWithAllDetailsAsync(c.Id, c.AcademicYearId)).FirstOrDefault();
        return Ok(new { isSuccess = true, data = MapClass(c, enrollments, cst) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassWithStudentsRequest request)
    {
        var academicYear = await _uow.AcademicYears.GetCurrentAsync();
        if (academicYear is null)
            return BadRequest(new { isSuccess = false, message = "لا توجد سنة دراسية نشطة" });

        var c = new SchoolClass
        {
            GradeLevelId = 1,
            AcademicYearId = academicYear.Id,
            Name = request.Name.Trim()
        };
        await _uow.Classes.AddAsync(c);
        await _uow.SaveChangesAsync();

        var students = new List<Student>();
        foreach (var sName in request.Students.Select(s => s.Trim()).Where(s => s.Length > 0))
        {
            var existing = (await _uow.Students.FindAsync(s => s.FullName == sName)).FirstOrDefault();
            if (existing is null)
            {
                existing = new Student { FullName = sName, IsActive = true };
                await _uow.Students.AddAsync(existing);
                await _uow.SaveChangesAsync();
            }
            var enrolled = (await _uow.StudentEnrollments.FindAsync(e => e.StudentId == existing.Id && e.ClassId == c.Id && e.AcademicYearId == academicYear.Id)).FirstOrDefault();
            if (enrolled is null)
            {
                await _uow.StudentEnrollments.AddAsync(new StudentEnrollment
                {
                    StudentId = existing.Id,
                    ClassId = c.Id,
                    AcademicYearId = academicYear.Id,
                    EnrolledAt = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }
            students.Add(existing);
        }
        await _uow.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(request.Subject) || !string.IsNullOrWhiteSpace(request.Teacher))
        {
            var subject = string.IsNullOrWhiteSpace(request.Subject) ? null
                : (await _uow.Subjects.FindAsync(s => s.Name == request.Subject.Trim())).FirstOrDefault();
            var teacher = string.IsNullOrWhiteSpace(request.Teacher) ? null
                : (await _uow.Users.FindAsync(u => u.FullName == request.Teacher.Trim() && u.Role == UserRole.Teacher)).FirstOrDefault();
            if (subject is not null && teacher is not null)
            {
                var exists = (await _uow.ClassSubjectTeachers.FindAsync(t =>
                    t.ClassId == c.Id && t.SubjectId == subject.Id && t.TeacherId == teacher.Id && t.AcademicYearId == academicYear.Id)).FirstOrDefault();
                if (exists is null)
                {
                    await _uow.ClassSubjectTeachers.AddAsync(new ClassSubjectTeacher
                    {
                        ClassId = c.Id,
                        SubjectId = subject.Id,
                        TeacherId = teacher.Id,
                        AcademicYearId = academicYear.Id
                    });
                    await _uow.SaveChangesAsync();
                }
            }
        }

        var enrollments = await _uow.StudentEnrollments.GetByClassWithStudentAsync(c.Id, academicYear.Id);
        var cst = (await _uow.ClassSubjectTeachers.GetByClassWithAllDetailsAsync(c.Id, academicYear.Id)).FirstOrDefault();
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, new { isSuccess = true, data = MapClass(c, enrollments, cst) });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _uow.Classes.GetByIdAsync(id);
        if (c is null || c.IsDeleted)
            return NotFound(new { isSuccess = false, message = "الفصل غير موجود" });
        _uow.Classes.SoftDelete(c);
        await _uow.SaveChangesAsync();
        return Ok(new { isSuccess = true, message = "تم حذف الفصل" });
    }

    private object MapClass(SchoolClass c, IReadOnlyList<StudentEnrollment> enrollments, ClassSubjectTeacher? cst)
    {
        return new
        {
            id = c.Id,
            name = c.Name,
            template_id = 1,
            teacher = cst?.Teacher?.FullName ?? "",
            subject = cst?.Subject?.Name ?? "",
            year = c.AcademicYear?.Name ?? "",
            gradeLevelId = c.GradeLevelId,
            gradeLevelName = c.GradeLevel?.Name ?? "",
            academicYearId = c.AcademicYearId,
            academicYearName = c.AcademicYear?.Name ?? "",
            students = enrollments.Select(e => new { id = e.Student.Id, name = e.Student.FullName, enrollmentId = e.Id }).ToList()
        };
    }
}
