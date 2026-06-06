using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Feedback;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[ApiController]
[Route("api/lesson-feedback")]
[Authorize(Roles = "Admin,Teacher,Student")]
public class LessonFeedbackController : ControllerBase
{
    private readonly ILessonFeedbackService _feedbackService;

    public LessonFeedbackController(ILessonFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest request)
    {
        var result = await _feedbackService.SubmitLessonFeedbackAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("summary/{classSubjectTeacherId:int}")]
    public async Task<IActionResult> GetSummary(int classSubjectTeacherId, [FromQuery] DateFilter filter)
    {
        var result = await _feedbackService.GetFeedbackSummaryByTeacherAsync(classSubjectTeacherId, filter);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("raw/{classSubjectTeacherId:int}")]
    public async Task<IActionResult> GetRaw(int classSubjectTeacherId, [FromQuery] DateFilter filter)
    {
        var result = await _feedbackService.GetFeedbackByTeacherRawAsync(classSubjectTeacherId, filter);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-lesson/{classSubjectTeacherId:int}")]
    public async Task<IActionResult> GetByLessonDate(int classSubjectTeacherId, [FromQuery] DateOnly lessonDate)
    {
        var result = await _feedbackService.GetFeedbackByLessonDateAsync(classSubjectTeacherId, lessonDate);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("by-enrollment/{enrollmentId:int}")]
    public async Task<IActionResult> GetByEnrollment(int enrollmentId)
    {
        var result = await _feedbackService.GetFeedbackByEnrollmentAsync(enrollmentId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("overall-rating/{teacherId:int}")]
    public async Task<IActionResult> GetOverallRating(int teacherId, [FromQuery] int academicYearId)
    {
        var result = await _feedbackService.GetOverallTeacherRatingAsync(teacherId, academicYearId);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int callerUserId)
    {
        var result = await _feedbackService.DeleteFeedbackAsync(id, callerUserId);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFeedbackRequest request)
    {
        if (id != request.Id)
            return BadRequest("معرف التقييم في الرابط لا يطابق المعرف في الطلب");

        var result = await _feedbackService.UpdateFeedbackAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
