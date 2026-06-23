using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public class EnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollmentService _enrollmentService;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentsController(IStudentEnrollmentService enrollmentService, IUnitOfWork unitOfWork)
    {
        _enrollmentService = enrollmentService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// يتحقق من أن المستخدم الحالي (طالب/ولي أمر) يملك حق الوصول إلى الطالب المطلوب.
    /// يُتجاوز الفحص للمدير والمعلم. يُرجع false مع رسالة إذا لم يكن مالكاً.
    /// </summary>
    private async Task<(bool Allowed, string? Error)> EnsureCanAccessStudentAsync(int studentId)
    {
        // المدير والمعلم لهم صلاحية كاملة على كل الطلاب
        if (User.IsInRole("Admin") || User.IsInRole("Teacher"))
            return (true, null);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return (false, "تعذّر التحقق من هوية المستخدم.");

        // الطالب: لا يصل إلا لبياناته الخاصة
        if (User.IsInRole("Student"))
        {
            var ownStudent = await _unitOfWork.Students.GetByUserIdAsync(userId);
            if (ownStudent is null || ownStudent.Id != studentId)
                return (false, "لا تملك صلاحية الوصول إلى بيانات هذا الطالب.");
            return (true, null);
        }

        // ولي الأمر: لا يصل إلا لأبنائه
        if (User.IsInRole("Parent"))
        {
            var isRelated = await _unitOfWork.ParentStudents.ExistsByParentAndStudentAsync(userId, studentId);
            if (!isRelated)
                return (false, "لا تملك صلاحية الوصول إلى بيانات هذا الطالب.");
            return (true, null);
        }

        return (false, "لا تملك صلاحية الوصول إلى بيانات هذا الطالب.");
    }

    /// <summary>
    /// يُرجع استجابة 403 مع رسالة عربية في نفس شكل OperationResult
    /// حتى يلتقطها errorInterceptor في الواجهة ويعرضها بشكل موحّد.
    /// </summary>
    private static IActionResult Forbidden(string message)
        => new ObjectResult(new { isSuccess = false, message, statusCode = 403 })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentRequest request)
    {
        var result = await _enrollmentService.EnrollStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> BulkEnroll([FromBody] BulkEnrollStudentsRequest request)
    {
        var result = await _enrollmentService.BulkEnrollStudentsAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("transfer")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Transfer([FromBody] TransferStudentRequest request)
    {
        var result = await _enrollmentService.TransferStudentAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("transfer/bulk")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> BulkTransfer([FromBody] BulkTransferStudentsRequest request)
    {
        var result = await _enrollmentService.BulkTransferStudentsAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("by-student/{studentId:int}")]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        var (allowed, error) = await EnsureCanAccessStudentAsync(studentId);
        if (!allowed)
            return Forbidden(error ?? "تم رفض الوصول.");

        var result = await _enrollmentService.GetEnrollmentsByStudentAsync(studentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("active/{studentId:int}")]
    public async Task<IActionResult> GetActive(int studentId, [FromQuery] int academicYearId)
    {
        var (allowed, error) = await EnsureCanAccessStudentAsync(studentId);
        if (!allowed)
            return Forbidden(error ?? "تم رفض الوصول.");

        var result = await _enrollmentService.GetActiveEnrollmentByStudentAsync(studentId, academicYearId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetByClass(int classId, [FromQuery] int academicYearId, [FromQuery] bool activeOnly = true)
    {
        var result = await _enrollmentService.GetEnrollmentsByClassAsync(classId, academicYearId, activeOnly);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-class/{classId:int}/paged")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetByClassPaged(
        int classId,
        [FromQuery] int academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _enrollmentService.GetEnrollmentsByClassPagedAsync(classId, academicYearId, page, pageSize, activeOnly, searchTerm);
        if (!result.IsSuccess)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    [HttpGet("transfers-history")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetTransferHistory(
        [FromQuery] int academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _enrollmentService.GetTransferHistoryAsync(academicYearId, page, pageSize);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
