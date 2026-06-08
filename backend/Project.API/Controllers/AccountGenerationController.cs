using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.BLL.DTOs.AccountGeneration;
using Project.BLL.Interfaces;

namespace Project.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/account-generation")]
public class AccountGenerationController : ControllerBase
{
    private readonly IAccountGenerationService _accountGenerationService;

    public AccountGenerationController(IAccountGenerationService accountGenerationService)
    {
        _accountGenerationService = accountGenerationService;
    }

    [HttpGet("student-candidates")]
    public async Task<IActionResult> GetStudentCandidates()
    {
        var result = await _accountGenerationService.GetStudentAccountCandidatesAsync();
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("students/generate")]
    public async Task<IActionResult> GenerateStudentAccount([FromBody] GenerateStudentAccountRequest request)
    {
        var result = await _accountGenerationService.GenerateStudentAccountAsync(request.StudentId);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("students/generate-bulk")]
    public async Task<IActionResult> GenerateBulkStudentAccounts([FromBody] BulkStudentAccountsRequest request)
    {
        var result = await _accountGenerationService.GenerateBulkStudentAccountsAsync(request.StudentIds);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("parents/create-with-students")]
    public async Task<IActionResult> CreateParentWithStudents([FromBody] CreateParentWithStudentsRequest request)
    {
        var result = await _accountGenerationService.CreateParentWithStudentsAsync(request);
        if (!result.IsSuccess)
            return BadRequest(result);

        return StatusCode(201, result);
    }
}
