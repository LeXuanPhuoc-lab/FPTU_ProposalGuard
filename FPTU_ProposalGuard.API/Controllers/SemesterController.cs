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
    [HttpGet(APIRoute.Semester.GetCurrentSemester, Name = nameof(GetCurrentSemester))]
    public async Task<IActionResult> GetCurrentSemester()
    {
        var result = await questionService.GetCurrentSemester();
        return Ok(result);
    }
    
    [Authorize]
    [HttpGet(APIRoute.Semester.GetSemesters, Name = nameof(GetSemesters))]
    public async Task<IActionResult> GetSemesters()
    {
        var result = await questionService.GetSemesters();
        return Ok(result);
    }
}