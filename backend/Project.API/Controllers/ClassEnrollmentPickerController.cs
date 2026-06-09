using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.ClassEnrollmentPicker;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

/// <summary>
/// Controller مستقل لميزة إضافة الطلاب لفصل عبر picker.
/// EnrollmentsController الأصلي لم يُلمَس.
/// </summary>
[ApiController]
[Route("api/class-enrollment-picker")]
[Authorize(Roles = "Admin,Teacher")]
public class ClassEnrollmentPickerController : ControllerBase
{
    private readonly IClassEnrollmentPickerService _service;

    public ClassEnrollmentPickerController(IClassEnrollmentPickerService service)
        => _service = service;

    /// <summary>
    /// GET api/class-enrollment-picker/{classId}/available-students
    /// يجيب الطلاب الغير مسجلين في أي فصل
    /// </summary>
    [HttpGet("{classId:int}/available-students")]
    public async Task<IActionResult> GetAvailableStudents(
        int classId,
        [FromQuery] GetAvailableStudentsFilter filter)
    {
        var result = await _service.GetAvailableStudentsAsync(classId, filter);
        if (!result.IsSuccess)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// POST api/class-enrollment-picker/bulk-enroll
    /// يسجّل طلاب في فصل بدون إرسال AcademicYearId
    /// </summary>
    [HttpPost("bulk-enroll")]
    public async Task<IActionResult> BulkEnroll([FromBody] ClassPickerBulkEnrollRequest request)
    {
        var result = await _service.BulkEnrollAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
