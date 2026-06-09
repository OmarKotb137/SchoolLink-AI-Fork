using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.ClassEnrollmentPicker;
using Project.BLL.DTOs.Enrollments;

namespace Project.BLL.Interfaces;

/// <summary>
/// Contract خاص بميزة إضافة الطلاب لفصل عبر picker.
/// مستقل تماماً عن IStudentEnrollmentService الموجود.
/// </summary>
public interface IClassEnrollmentPickerService
{
    /// <summary>يجيب الطلاب الغير مسجلين في أي فصل مع pagination وفلتر وترتيب.</summary>
    Task<OperationResult<PagedResult<AvailableStudentDto>>> GetAvailableStudentsAsync(
        int classId,
        GetAvailableStudentsFilter filter);

    /// <summary>يسجّل طلاب في فصل — يستنتج AcademicYearId من الفصل تلقائياً.</summary>
    Task<OperationResult<BulkEnrollResultDto>> BulkEnrollAsync(
        ClassPickerBulkEnrollRequest request);
}
