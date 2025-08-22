using FPTU_ProposalGuard.API.Payloads;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ProposalGuard.API.Controllers;

public class QuestionController(
    IQuestionService questionService
) : ControllerBase
{
    [Authorize]
    [HttpGet(APIRoute.Question.GetAllReviewQuestions, Name = nameof(GetAllReviewQuestions))]
    public async Task<IActionResult> GetAllReviewQuestions()
    {
        var result = await questionService.GetAll();
        return Ok(result);
    }
}