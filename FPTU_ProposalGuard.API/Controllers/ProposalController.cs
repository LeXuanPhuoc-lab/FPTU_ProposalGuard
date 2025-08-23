using System.Security.Claims;
using FPTU_ProposalGuard.API.Extensions;
using FPTU_ProposalGuard.API.Payloads;
using FPTU_ProposalGuard.API.Payloads.Requests.Proposals;
using FPTU_ProposalGuard.Application.Configurations;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;

namespace FPTU_ProposalGuard.API.Controllers;

public class ProposalController(
    IProposalService proposalService,
    IProjectProposalService<ProjectProposalDto> projectProposalService,
    IOptionsMonitor<AppSettings> monitor
) : ControllerBase
{
    private readonly AppSettings _appSettings = monitor.CurrentValue;

    [Authorize]
    [HttpPost(APIRoute.Proposal.AddProposalsWithFiles, Name = nameof(AddProposalsWithFiles))]
    public async Task<IActionResult> AddProposalsWithFiles([FromForm] AddProposalsWithFilesRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        return Ok(await proposalService.AddProposalsWithFiles(req.ToTupleList(),
            req.SemesterId, email));
    }

    // [Authorize]
    // [HttpPost(APIRoute.Proposal.AddProposals, Name = nameof(UploadEmbeddedWithoutFile))]
    // public async Task<IActionResult> UploadEmbeddedWithoutFile([FromForm] AddProposalsRequest req)
    // {
    //     var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    //     return Ok(await proposalService.AddProposals(req.ToTupleList(), req.SemesterId, email!));
    // }


    [Authorize]
    [HttpPost(APIRoute.Proposal.CheckDuplicatedProposal, Name = nameof(CheckDuplicatedProposal))]
    public async Task<IActionResult> CheckDuplicatedProposal([FromForm] CheckDuplicatedProposalRequest req)
    {
        return Ok(await proposalService.CheckDuplicatedProposal(req.Files));
    }

    [Authorize]
    [HttpPut(APIRoute.Proposal.UpdateStatus, Name = nameof(UpdateStatus))]
    public async Task<IActionResult> UpdateStatus([FromRoute] int proposalId, [FromQuery] bool isApproved)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        return Ok(await proposalService.UpdateStatus(proposalId, isApproved, email));
    }

    [Authorize]
    [HttpPost(APIRoute.Proposal.ReUploadProposal, Name = nameof(ReUploadProposal))]
    public async Task<IActionResult> ReUploadProposal([FromForm] ReUploadRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        return Ok(await proposalService.ReUploadProposal(req.ToTuple(), req.ProjectProposalId, email, req.SemesterId));
    }

    [Authorize]
    [HttpGet(APIRoute.Proposal.GetFile, Name = nameof(GetFile))]
    public async Task<IActionResult> GetFile([FromRoute] int historyId)
    {
        var result =
            ((Stream Stream, string ContentType, string FileName))(await proposalService.GetFile(historyId)).Data!;
        return File(result.Stream, result.ContentType, result.FileName);
    }

    [Authorize]
    [HttpGet(APIRoute.Proposal.GetAll, Name = nameof(GetAll))]
    public async Task<IActionResult> GetAll([FromQuery] ProposalSpecParams specParams)
    {
        var result = await projectProposalService.GetAllWithSpecAsync(new ProposalSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));

        return Ok(result);
    }

    [Authorize]
    [HttpGet(APIRoute.Proposal.GetAllUploaded, Name = nameof(GetAllUploaded))]
    public async Task<IActionResult> GetAllUploaded([FromQuery] ProposalSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        var result = await projectProposalService.GetAllWithSpecAsync(new ProposalSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize,
            email));

        return Ok(result);
    }

    [Authorize]
    [HttpGet(APIRoute.Proposal.GetById, Name = nameof(GetById))]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        return Ok(await projectProposalService.GetByIdAsync(id));
    }

    [HttpPost(APIRoute.Proposal.AddReviewers, Name = nameof(AddReviewers))]
    public async Task<IActionResult> AddReviewers([FromBody] AddReviewersRequest req)
    {
        return Ok(await proposalService.AddReviewers(req.Proposals.ToDictionary(p => p.ProposalId,
            p => p.ReviewerEmails)));
    }

    [HttpPost(APIRoute.Proposal.SubmitReview, Name = nameof(SubmitReview))]
    public async Task<IActionResult> SubmitReview([FromRoute] int id, [FromBody] SubmitReviewRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        return Ok(await proposalService.SubmitReview<ReviewSessionDto>(id
            , req.ToDto() ,email));
    }
}