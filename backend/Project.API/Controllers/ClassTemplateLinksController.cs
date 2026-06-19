using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Teacher")]
public class ClassTemplateLinksController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ClassTemplateLinksController(IUnitOfWork uow) { _uow = uow; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var links = await _uow.ClassTemplateLinks.FindAsync(_ => true);
        var result = new List<object>();
        foreach (var link in links)
        {
            var cls = await _uow.Classes.GetByIdWithIncludesAsync(link.ClassId);
            var enrollments = cls != null
                ? await _uow.StudentEnrollments.GetByClassWithStudentAsync(cls.Id, link.AcademicYearId)
                : new List<StudentEnrollment>();
            var template = (await _uow.EvaluationTemplates.FindAsync(t => t.Id == link.TemplateId)).FirstOrDefault();

            // Find the CST that matches the linked template's subject
            string subject = "", teacher = "";
            int? subjectId = null;
            if (cls != null && template != null)
            {
                var csts = await _uow.ClassSubjectTeachers.GetByClassWithAllDetailsAsync(cls.Id, link.AcademicYearId);
                var matched = csts.FirstOrDefault(cst => cst.SubjectId == template.SubjectId);
                if (matched != null)
                {
                    subject = matched.Subject?.Name ?? "";
                    subjectId = matched.SubjectId;
                    teacher = matched.Teacher?.FullName ?? "";
                }
            }

            result.Add(new
            {
                id = link.Id,
                classId = link.ClassId,
                templateId = link.TemplateId,
                academicYearId = link.AcademicYearId,
                className = cls?.Name ?? "",
                subject,
                subjectId,
                teacher,
                templateName = template?.Name ?? "",
                students = enrollments.Select(e => new { id = e.Student.Id, name = e.Student.FullName, enrollmentId = e.Id }).ToList()
            });
        }
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLinkRequest request)
    {
        var existing = (await _uow.ClassTemplateLinks.FindAsync(l =>
            l.ClassId == request.ClassId && l.TemplateId == request.TemplateId && l.AcademicYearId == request.AcademicYearId))
            .FirstOrDefault();

        if (existing != null)
            return Conflict(new { isSuccess = false, message = "الربط موجود مسبقاً" });

        var link = new ClassTemplateLink
        {
            ClassId = request.ClassId,
            TemplateId = request.TemplateId,
            AcademicYearId = request.AcademicYearId
        };
        await _uow.ClassTemplateLinks.AddAsync(link);
        await _uow.SaveChangesAsync();
        return Ok(new { isSuccess = true, data = new { id = link.Id } });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var link = await _uow.ClassTemplateLinks.GetByIdAsync(id);
        if (link is null || link.IsDeleted)
            return NotFound(new { isSuccess = false, message = "الربط غير موجود" });
        _uow.ClassTemplateLinks.SoftDelete(link);
        await _uow.SaveChangesAsync();
        return Ok(new { isSuccess = true, message = "تم حذف الربط" });
    }
}

public class CreateLinkRequest
{
    public int ClassId { get; set; }
    public int TemplateId { get; set; }
    public int AcademicYearId { get; set; }
}
