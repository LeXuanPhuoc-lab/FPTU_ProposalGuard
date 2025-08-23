using System.Security.Claims;
using FPTU_ProposalGuard.Application.Dtos.Reviews;

namespace FPTU_ProposalGuard.API.Controllers;

using FPTU_ProposalGuard.API.Payloads;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class ReviewSessionController(
    IReviewSessionService<ReviewSessionDto> reviewSessionService
) : ControllerBase
{
    [Authorize]
    [HttpGet(APIRoute.ReviewSession.GetSessionsToBeReviewed, Name = nameof(GetSessionsToBeReviewed))]
    public async Task<IActionResult> GetSessionsToBeReviewed()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        var result = await reviewSessionService.GetSessionsToBeReviewed(email);
        return Ok(result);
    }
}