using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpGet("class-subject-teacher/{classSubjectTeacherId}")]
        public async Task<IActionResult> GetAllByClassSubjectTeacher(int classSubjectTeacherId)
        {
            var result = await _examService.GetAllByClassSubjectTeacherAsync(classSubjectTeacherId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _examService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExamDto dto)
        {
            var result = await _examService.CreateAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateExamDto dto)
        {
            var result = await _examService.UpdateAsync(dto);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _examService.DeleteAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPut("{id}/publish")]
        public async Task<IActionResult> Publish(int id)
        {
            var result = await _examService.PublishAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}/unpublish")]
        public async Task<IActionResult> Unpublish(int id)
        {
            var result = await _examService.UnPublishAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("ai-generate")]
        public async Task<IActionResult> CreateFromAi([FromBody] CreateExamFromAiDto dto)
        {
            var result = await _examService.CreateFromAiAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{uid}/html")]
        [AllowAnonymous]
        public async Task<IActionResult> RenderHtml(Guid uid)
        {
            var result = await _examService.RenderHtmlAsync(uid);
            if (!result.IsSuccess)
                return NotFound(result);

            return Content(result.Data, "text/html; charset=utf-8");
        }

        [HttpPut("{uid}/save")]
        public async Task<IActionResult> SaveQuestions(Guid uid, [FromBody] SaveExamQuestionsDto dto)
        {
            dto.Uid = uid;
            var result = await _examService.SaveExamQuestionsAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
