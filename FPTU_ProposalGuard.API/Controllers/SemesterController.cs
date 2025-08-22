using FPTU_ProposalGuard.API.Payloads;
using FPTU_ProposalGuard.Application.Dtos.Semesters;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace FPTU_ProposalGuard.API.Controllers;

public class SemesterController(
    ISemesterService<SemesterDto> questionService
) : ControllerBase
{
    [Authorize]
    [HttpGet(APIRoute.Semester.GetCurrentSemesterCode, Name = nameof(GetCurrentSemesterCode))]
    public async Task<IActionResult> GetCurrentSemesterCode()
    {
        var result = await questionService.GetCurrentSemesterCode();
        return Ok(result);
    }
}