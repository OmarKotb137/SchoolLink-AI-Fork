using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.Interfaces;
using Project.BLL.Services;
using Project.DAL.Interfaces;

namespace Project.API.Controllers;

[Route("api/question-embedding")]
[ApiController]
public class QuestionEmbeddingController : ControllerBase
{
    private readonly IQuestionEmbeddingService _questionEmbeddingService;
    private readonly IQuestionBankService _questionBankService;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionEmbeddingController(
        IQuestionEmbeddingService questionEmbeddingService,
        IQuestionBankService questionBankService,
        IUnitOfWork unitOfWork)
    {
        _questionEmbeddingService = questionEmbeddingService;
        _questionBankService = questionBankService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>تضمين أسئلة امتحان في MongoDB (مع حفظها في بنك الأسئلة أولاً)</summary>
    [HttpPost("from-exam/{examId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> EmbedFromExam(int examId, [FromQuery] int? subjectId = null)
    {
        // Resolve subjectId from exam if not provided
        if (!subjectId.HasValue || subjectId == 0)
        {
            var exam = await _unitOfWork.Exams.GetWithQuestionsAsync(examId);
            if (exam == null)
                return NotFound(new { isSuccess = false, message = "الامتحان غير موجود" });
            subjectId = exam.SubjectId ?? exam.ClassSubjectTeacher?.SubjectId ?? 0;
            if (subjectId == 0)
                return BadRequest(new { isSuccess = false, message = "لم يتم تحديد المادة" });
        }

        // 1. Save exam questions to QuestionBank first
        var saveResult = await _questionBankService.BulkAddFromExamAsync(examId, subjectId.Value);
        if (!saveResult.IsSuccess)
            return BadRequest(saveResult);

        // 2. Get QB IDs from ExamQuestionBankItem links
        var links = await _unitOfWork.ExamQuestionBankItems
            .FindAsync(l => l.ExamId == examId && !l.IsDeleted);
        var qbIds = links.Select(l => l.QuestionBankId).Distinct().ToList();

        if (qbIds.Count == 0)
            return BadRequest(new { isSuccess = false, message = "لم يتم العثور على أسئلة مضاف لبنك الأسئلة" });

        // 3. Embed
        var result = await _questionEmbeddingService.EmbedQuestionBankItemsAsync(qbIds);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(new { isSuccess = true, data = result.Data, message = result.Message });
    }

    /// <summary>تضمين أسئلة من بنك الأسئلة مباشرة (بـ IDs)</summary>
    [HttpPost("from-question-bank")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> EmbedFromQuestionBank([FromBody] List<int> questionBankIds)
    {
        var result = await _questionEmbeddingService.EmbedQuestionBankItemsAsync(questionBankIds);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>بحث دلالي في الأسئلة المضمنة</summary>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromBody] SemanticSearchRequest request)
    {
        var result = await _questionEmbeddingService.SemanticSearchAsync(request);
        return Ok(result);
    }

    /// <summary>إضافة كل الأسئلة غير المضمنة في بنك الأسئلة</summary>
    [HttpPost("embed-all")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> EmbedAll()
    {
        var result = await _questionEmbeddingService.EmbedAllUnembeddedAsync();
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
