using System.Text;
using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Configurations;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Amazon.S3.Model;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.Semesters;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Application.Services.IExternalServices;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using OpenSearch.Net;
using Serilog;
using ProjectProposalDto = FPTU_ProposalGuard.Application.Dtos.Proposals.ProjectProposalDto;

namespace FPTU_ProposalGuard.Application.Services;

public class ProposalService : IProposalService
{
    private readonly ILogger _logger;
    private readonly CheckProposalSettings _appSettings;
    private readonly ISystemMessageService _msgService;
    private readonly IExtractService _extractService;
    private readonly IProjectProposalService<ProjectProposalDto> _projectService;
    private readonly IProposalSupervisorService<ProposalSupervisorDto> _supervisorService;
    private readonly IProposalStudentService<ProposalStudentDto> _studentService;
    private readonly IProposalHistoryService<ProposalHistoryDto> _historyService;
    private readonly IUserService<UserDto> _userService;
    private readonly OpenSearchLowLevelClient _openSearchClient;
    private readonly IS3Service _s3;
    private readonly ISemesterService<SemesterDto> _semesterService;
    private readonly IReviewSessionService<ReviewSessionDto> _reviewSessionService;

    public ProposalService(
        ILogger logger,
        IOptionsMonitor<CheckProposalSettings> appSettings,
        ISystemMessageService msgService,
        IExtractService extractService,
        IProjectProposalService<ProjectProposalDto> projectService,
        IProposalSupervisorService<ProposalSupervisorDto> supervisorService,
        IProposalStudentService<ProposalStudentDto> studentService,
        IProposalHistoryService<ProposalHistoryDto> historyService,
        IS3Service s3,
        ISemesterService<SemesterDto> semesterService,
        IReviewSessionService<ReviewSessionDto> reviewSessionService,
        IUserService<UserDto> userService)
    {
        _logger = logger;
        _appSettings = appSettings.CurrentValue;
        _msgService = msgService;
        _extractService = extractService;
        _projectService = projectService;
        _supervisorService = supervisorService;
        _studentService = studentService;
        _historyService = historyService;
        _userService = userService;
        _s3 = s3;
        _semesterService = semesterService;
        _reviewSessionService = reviewSessionService;
        // Khởi tạo OpenSearch client
        var node = new Uri(_appSettings.OpenSearchUrl);

        var config = new ConnectionConfiguration(node)
            .BasicAuthentication(_appSettings.OpenSearchUsername, _appSettings.OpenSearchPassword)
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll); // nếu cần bỏ qua SSL cert

        _openSearchClient = new OpenSearchLowLevelClient(config);
    }

    // public async Task<IServiceResult> AddProposalsWithFiles(List<IFormFile> files, int semesterId, string email,
    //     List<ProposalHistory>? histories = null)
    // {
    //     try
    //     {
    //         var userResponse = await _userService.GetCurrentUserAsync(email);
    //         if (userResponse.ResultCode != ResultCodeConst.SYS_Success0002)
    //         {
    //             return userResponse;
    //         }
    //
    //         var extractedDocuments = await _extractService.ExtractFullContentDocuments(files);
    //
    //         var user = (userResponse.Data as UserDto)!;
    //
    //         var proposalDtos = new List<ProjectProposalDto>();
    //         foreach (var extractedDocument in extractedDocuments)
    //         {
    //             var projectProposalDto = new ProjectProposalDto()
    //             {
    //                 SemesterId = semesterId,
    //                 SubmitterId = user.UserId,
    //                 VieTitle = extractedDocument.VieTitle,
    //                 EngTitle = extractedDocument.EngTitle,
    //                 ContextText = extractedDocument.Context,
    //                 SolutionText = extractedDocument.Solution,
    //                 DurationFrom = DateOnly.TryParse(extractedDocument.DurationFrom, out var durationFrom)
    //                     ? durationFrom
    //                     : DateOnly.FromDateTime(DateTime.Today),
    //                 DurationTo = DateOnly.TryParse(extractedDocument.DurationTo, out var durationTo)
    //                     ? durationTo
    //                     : DateOnly.FromDateTime(DateTime.Today.AddMonths(4)),
    //                 Status = ProjectProposalStatus.Pending,
    //                 Abbreviation = extractedDocument.Abbreviation,
    //
    //                 CreatedAt = DateTime.UtcNow,
    //                 CreatedBy = user.UserId.ToString(),
    //             };
    //             var innerSupervisorJson = JsonDocument.Parse(extractedDocument.Supervisors)
    //                 .RootElement.GetRawText();
    //
    //             var supervisors = JsonSerializer
    //                 .Deserialize<List<ExtractedProposalSupervisorDto>>(innerSupervisorJson, new JsonSerializerOptions
    //                 {
    //                     PropertyNameCaseInsensitive = true
    //                 })?
    //                 .Select(sup => new ProposalSupervisorDto
    //                 {
    //                     FullName = sup.FullName,
    //                     Email = sup.Email,
    //                     Phone = sup.Phone
    //                 }).ToList() ?? new List<ProposalSupervisorDto>();
    //
    //
    //             var innerStudentJson = JsonDocument.Parse(extractedDocument.Students)
    //                 .RootElement.GetRawText();
    //             var students = JsonSerializer
    //                 .Deserialize<List<ExtractedProposalStudentDto>>(innerStudentJson, new JsonSerializerOptions
    //                 {
    //                     PropertyNameCaseInsensitive = true
    //                 })?
    //                 .Select(student =>
    //                     new ProposalStudentDto
    //                     {
    //                         FullName = student.FullName,
    //                         StudentCode = student.StudentCode,
    //                         Email = student.Email,
    //                         Phone = student.Phone,
    //                         RoleInGroup = null
    //                     }).ToList() ?? new List<ProposalStudentDto>();
    //             projectProposalDto.ProposalSupervisors = supervisors;
    //             projectProposalDto.ProposalStudents = students;
    //
    //             proposalDtos.Add(projectProposalDto);
    //         }
    //
    //         // // Extract proposal details from the documents
    //         // var proposalDtos = extractedDocuments
    //         //     .Select(x => new ProjectProposalDto
    //         //     {
    //         //         SemesterId = semesterId,
    //         //         SubmitterId = user.UserId,
    //         //         VieTitle = string.Empty,
    //         //         EngTitle = x.EngTitle,
    //         //         ContextText = x.Context,
    //         //         SolutionText = x.Solution,
    //         //         DurationFrom = DateOnly.FromDateTime(DateTime.Today),
    //         //         DurationTo = DateOnly.FromDateTime(DateTime.Today.AddMonths(4)),
    //         //         Status = ProjectProposalStatus.Pending,
    //         //         CreatedAt = DateTime.UtcNow,
    //         //         CreatedBy = user.UserId.ToString(),
    //         //     }).ToList();
    //         // // Extract proposal supervisors from the documents
    //         // var allSupervisors = extractedDocuments
    //         //     .Zip(proposalDtos, (doc, proposal) =>
    //         //         JsonSerializer.Deserialize<List<ExtractedProposalSupervisorDto>>(doc.Supervisors)?.Select(sup =>
    //         //             new ProposalSupervisorDto
    //         //             {
    //         //                 ProjectProposalId = proposal.ProjectProposalId,
    //         //                 FullName = sup.FullName,
    //         //                 Email = sup.Email,
    //         //                 Phone = sup.Phone
    //         //             }) ?? new List<ProposalSupervisorDto>()
    //         //     ).SelectMany(x => x).ToList();
    //         //
    //         //
    //         // // Extract proposal students from the documents
    //         //
    //         // var allStudents = extractedDocuments
    //         //     .Zip(proposalDtos, (doc, proposal) =>
    //         //         JsonSerializer.Deserialize<List<ExtractedProposalStudentDto>>(doc.Students)?.Select(student =>
    //         //             new ProposalStudentDto
    //         //             {
    //         //                 ProjectProposalId = proposal.ProjectProposalId,
    //         //                 FullName = student.FullName,
    //         //                 StudentCode = student.StudentCode,
    //         //                 Email = student.Email,
    //         //                 Phone = student.Phone,
    //         //                 RoleInGroup = null
    //         //             }) ?? new List<ProposalStudentDto>()
    //         //     ).SelectMany(x => x).ToList();
    //         /*
    //         Then upload to s3, create history and similarity results.
    //         */
    //         // Check if Supervisor is existed or not
    //         // var supervisorDtos = allSupervisors
    //         //     .Distinct().ToList();
    //         // foreach (var proposalSupervisorDto in supervisorDtos)
    //         // {
    //         //     var notExistedSupervisor = new List<string>();
    //         //     var supervisorResponse = await _supervisorService.GetByEmailAsync(proposalSupervisorDto.Email);
    //         //     if (supervisorResponse.ResultCode == ResultCodeConst.SYS_Success0002)
    //         //     {
    //         //         proposalSupervisorDto.SupervisorNo = (supervisorResponse.Data as ProposalSupervisorDto)!.SupervisorNo;
    //         //     }
    //         //     
    //         // }
    //
    //         var createProposalResult = await _projectService.CreateManyAsync(proposalDtos);
    //
    //         if (createProposalResult.ResultCode != ResultCodeConst.SYS_Success0001)
    //         {
    //             return createProposalResult;
    //         }
    //
    //         var proposalEntities = (createProposalResult.Data as List<ProjectProposalDto>)!;
    //
    //         await UploadChunks(proposalEntities.Select((e, i) =>
    //             (e.ProjectProposalId, extractedDocuments[i].Text, e.EngTitle)).ToList());
    //
    //
    //         return new ServiceResult(ResultCodeConst.Proposal_Success0002,
    //             await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0002));
    //     }
    //     catch (Exception e)
    //     {
    //         return new ServiceResult(ResultCodeConst.Proposal_Warning0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0001) + ": " + e.Message);
    //     }
    // }

    // public async Task<IServiceResult> AddProposals(
    //     List<(string Name, string Context, string Solution, string Text)> proposals, int semesterId, string email)
    // {
    //     try
    //     {
    //         var userResponse = await _userService.GetCurrentUserAsync(email);
    //         if (userResponse.ResultCode != ResultCodeConst.SYS_Success0002)
    //         {
    //             return userResponse;
    //         }
    //
    //         var user = (userResponse.Data as UserDto)!;
    //
    //         var proposalDtos = proposals.Select(x => new ProjectProposalDto
    //         {
    //             SemesterId = semesterId,
    //             SubmitterId = user.UserId,
    //             VieTitle = string.Empty,
    //             EngTitle = x.Name,
    //             ContextText = x.Context,
    //             SolutionText = x.Solution,
    //             DurationFrom = DateOnly.FromDateTime(DateTime.Today),
    //             DurationTo = DateOnly.FromDateTime(DateTime.Today.AddMonths(4)),
    //             Status = ProjectProposalStatus.Pending,
    //             CreatedAt = DateTime.UtcNow,
    //             CreatedBy = user.UserId.ToString(),
    //         }).ToList();
    //
    //         var createProposalResult = await _projectService.CreateManyAsync(proposalDtos);
    //
    //         if (createProposalResult.ResultCode != ResultCodeConst.SYS_Success0001)
    //         {
    //             return createProposalResult;
    //         }
    //
    //         var proposalEntities = (createProposalResult.Data as List<ProjectProposalDto>)!;
    //
    //         await UploadChunks(proposalEntities.Select((e, i) =>
    //             (e.ProjectProposalId, proposals[i].Text, e.EngTitle)).ToList());
    //
    //         return new ServiceResult(ResultCodeConst.Proposal_Success0002,
    //             await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0002));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when progress upload data with file");
    //     }
    // }
    public async Task<IServiceResult> AddReviewers(IDictionary<int, List<string>> proposalReviewers)
    {
        try
        {
            List<(int, ProjectProposalDto)> proposalsToUpdate = new List<(int, ProjectProposalDto)>();

            //Get Role To update
            // var roleBaseSpec = new BaseSpecification<SystemRole>(x => x.RoleName == Role.Moderator.ToString());
            // var role = await _roleService.GetWithSpecAsync(roleBaseSpec);
            // if (role.ResultCode != ResultCodeConst.SYS_Success0002)
            // {
            //     return role;
            // }
            // var systemRole = (role.Data as SystemRoleDto)!;

            //Get users
            List<string> emails = proposalReviewers.Values.SelectMany(list => list).ToList();
            var userBaseSpec = await _userService.GetAllAsync();
            if (userBaseSpec.ResultCode != ResultCodeConst.SYS_Success0001)
            {
                return userBaseSpec;
            }

            var users = (userBaseSpec.Data as List<UserDto>)!;
            var userEmails = users.Select(x => x.Email).ToList();
            var notExistedUser = userEmails.Where(x => !emails.Contains(x)).ToList();

            var existedUsers = users.Where(x => emails.Contains(x.Email)).ToList();

            #region Add new users

            // add new users back to existed users

            #endregion

            List<(int, ProposalHistoryDto)> historyToUpdate = new List<(int, ProposalHistoryDto)>();
            foreach (var proposalId in proposalReviewers.Keys)
            {
                // get by id and latest version
                var proposalHistory = await _historyService.GetLatestHistoryByProposalIdAsync(proposalId);
                if (proposalHistory.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return proposalHistory;
                }

                var latestHistory = (proposalHistory.Data as List<ProposalHistoryDto>)!.MaxBy(x => x.Version);

                // create session
                latestHistory!.ReviewSessions = existedUsers.Select(x =>
                {
                    return new ReviewSessionDto
                    {
                        HistoryId = latestHistory.HistoryId,
                        ReviewerId = x.UserId,
                        ReviewStatus = ReviewStatus.Pending,
                    };
                }).ToList();

                // add history to update
                historyToUpdate.Add((latestHistory.HistoryId, latestHistory));
            }

            // Update history
            var updateResult = await _historyService.AddReviewersAsync(historyToUpdate);
            if (updateResult.ResultCode != ResultCodeConst.SYS_Success0001)
            {
                return updateResult;
            }

            return new ServiceResult(ResultCodeConst.Proposal_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0003));
        }
        catch (Exception ex)
        {
            return new ServiceResult(ResultCodeConst.Proposal_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Warning0001) + ": " + ex.Message);
        }
    }


    public async Task<IServiceResult> AddProposalsWithFiles<T>(List<(IFormFile file, T fileDetail)> files,
        int semesterId, string email) where T : class
    {
        var uploadedFileKeys = new List<string>();
        try
        {
            var userResponse = await _userService.GetCurrentUserAsync(email);
            if (userResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                return userResponse;

            var user = (userResponse.Data as UserDto)!;

            var proposalDtos = new List<ProjectProposalDto>();

            foreach (var (file, fileDetail) in files)
            {
                // Extract data from file
                var extracted = await _extractService.ExtractFullContentDocument(file);

                // Upload to S3
                var stream = file.OpenReadStream();
                var fileKey = $"{Guid.NewGuid()}_{file.FileName}_1";
                await _s3.UploadFile(stream, fileKey, file.ContentType);
                uploadedFileKeys.Add(fileKey);
                var students = JsonSerializer.Deserialize<List<ExtractedProposalStudentDto>>(extracted.Students,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })
                    ?
                    .Select(s => new ProposalStudentDto
                    {
                        FullName = s.FullName,
                        StudentCode = s.StudentCode,
                        Email = s.Email,
                        Phone = s.Phone
                    }).ToList() ?? new List<ProposalStudentDto>();
                var supervisors = JsonSerializer
                    .Deserialize<List<ExtractedProposalSupervisorDto>>(extracted.Supervisors, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?
                    .Select(s => new ProposalSupervisorDto
                    {
                        FullName = s.FullName,
                        Email = s.Email,
                        Phone = s.Phone
                    }).ToList() ?? new List<ProposalSupervisorDto>();
                // 3. Create Proposal
                var proposal = new ProjectProposalDto
                {
                    SemesterId = semesterId,
                    SubmitterId = user.UserId,
                    VieTitle = extracted.VieTitle,
                    EngTitle = extracted.EngTitle,
                    ContextText = extracted.Context,
                    SolutionText = extracted.Solution,
                    DurationFrom = DateOnly.TryParse(extracted.DurationFrom, out var from)
                        ? from
                        : DateOnly.FromDateTime(DateTime.Today),
                    DurationTo = DateOnly.TryParse(extracted.DurationTo, out var to)
                        ? to
                        : DateOnly.FromDateTime(DateTime.Today.AddMonths(4)),
                    Abbreviation = extracted.Abbreviation,
                    Status = ProjectProposalStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.UserId.ToString(),
                    ProposalStudents = students,
                    ProposalSupervisors = supervisors,
                    FunctionalRequirements =
                        JsonSerializer.Deserialize<List<string>>(extracted.FunctionalRequirements)!,
                    NonFunctionalRequirements =
                        JsonSerializer.Deserialize<List<string>>(extracted.NonFunctionalRequirements)!,
                    TechnicalStack = JsonSerializer.Deserialize<List<string>>(extracted.TechnicalStack)!,
                    Tasks = JsonSerializer.Deserialize<List<string>>(extracted.Tasks)!
                };

                // Get semester Detail
                var semesterResponse = await _semesterService.GetByIdAsync(semesterId);
                if (semesterResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    foreach (var key in uploadedFileKeys)
                    {
                        await _s3.DeleteFile(key);
                    }

                    return semesterResponse;
                }

                var semester = (semesterResponse.Data as SemesterDto)!;

                var proposalCode = await _historyService.GenerateProposalCodeAsync(semester.SemesterId
                    , semester.SemesterCode);
                if (proposalCode.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    foreach (var key in uploadedFileKeys)
                    {
                        await _s3.DeleteFile(key);
                    }

                    return proposalCode;
                }

                string proposalCodeValue = (proposalCode.Data as string)!;

                var history = fileDetail is ProposalHistoryDto dto
                    ? new ProposalHistoryDto
                    {
                        Status = dto.Status,
                        ProposalCode = proposalCodeValue,
                        Version = 1,
                        Url = fileKey,
                        ProcessById = user.UserId,
                        ProcessDate = DateTime.UtcNow,
                        Comment = dto.Comment,
                        SimilarProposals = dto.SimilarProposals
                    }
                    : new ProposalHistoryDto
                    {
                        Status = ProjectProposalStatus.Pending.ToString(),
                        Version = 1,
                        ProposalCode = proposalCodeValue,
                        Url = fileKey,
                        ProcessById = user.UserId,
                        ProcessDate = DateTime.UtcNow
                    };

                proposal.ProposalHistories.Add(history);
                proposalDtos.Add(proposal);
            }

            // Create Proposal
            var createResult = await _projectService.CreateManyAsync(proposalDtos);
            if (createResult.ResultCode != ResultCodeConst.SYS_Success0001)
            {
                foreach (var key in uploadedFileKeys)
                {
                    await _s3.DeleteFile(key);
                }

                return createResult;
            }

            return new ServiceResult(ResultCodeConst.Proposal_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0002));
        }
        catch (Exception ex)
        {
            foreach (var key in uploadedFileKeys)
            {
                await _s3.DeleteFile(key);
            }

            return new ServiceResult(ResultCodeConst.Proposal_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Warning0001) + ": " + ex.Message);
        }
    }

    public async Task<IServiceResult> ReUploadProposal<T>((IFormFile file, T fileDetail) file, int proposalId,
        string email, int semesterId) where T : class
    {
        string uploadKey = string.Empty;
        try
        {
            var userResponse = await _userService.GetCurrentUserAsync(email);
            if (userResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                return userResponse;

            var user = (userResponse.Data as UserDto)!;

            // Check if proposal exists
            var proposal = await _projectService.GetByIdAsync(proposalId);
            if (proposal.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return proposal;
            }

            var projectProposal = (proposal.Data as ProjectProposalDto)!;
            projectProposal.Status = ProjectProposalStatus.Pending;
            var latestVersion = projectProposal.ProposalHistories
                .Max(h => h.Version);

            var extracted = await _extractService.ExtractFullContentDocument(file.file);
            // Upload to S3
            var stream = file.file.OpenReadStream();
            var fileKey = $"{Guid.NewGuid()}_{file.file.FileName}_{latestVersion + 1}";
            await _s3.UploadFile(stream, fileKey, file.file.ContentType);
            uploadKey = fileKey;

            projectProposal.SemesterId = semesterId;
            projectProposal.VieTitle = extracted.VieTitle;
            projectProposal.EngTitle = extracted.EngTitle;
            projectProposal.ContextText = extracted.Context;
            projectProposal.SolutionText = extracted.Solution;
            projectProposal.DurationFrom = DateOnly.TryParse(extracted.DurationFrom, out var from)
                ? from
                : DateOnly.FromDateTime(DateTime.Today);
            projectProposal.DurationTo = DateOnly.TryParse(extracted.DurationTo, out var to)
                ? to
                : DateOnly.FromDateTime(DateTime.Today.AddMonths(4));
            projectProposal.Abbreviation = extracted.Abbreviation;
            projectProposal.Status = ProjectProposalStatus.Pending;
            projectProposal.FunctionalRequirements =
                JsonSerializer.Deserialize<List<string>>(extracted.FunctionalRequirements)!;
            projectProposal.NonFunctionalRequirements =
                JsonSerializer.Deserialize<List<string>>(extracted.NonFunctionalRequirements)!;
            projectProposal.TechnicalStack = JsonSerializer.Deserialize<List<string>>(extracted.TechnicalStack)!;
            projectProposal.Tasks = JsonSerializer.Deserialize<List<string>>(extracted.Tasks)!;

            // extract and check supervisors and students
            var extractedStudents = JsonSerializer
                .Deserialize<List<ExtractedProposalStudentDto>>(extracted.Students, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })?
                .Select(s => new ProposalStudentDto
                {
                    FullName = s.FullName,
                    StudentCode = s.StudentCode,
                    Email = s.Email,
                    Phone = s.Phone,
                    ProjectProposalId = proposalId
                }).ToList() ?? new List<ProposalStudentDto>();

            // sync students
            var studentTask = GetSyncPlan<ProposalStudentDto>(
                dtoList: projectProposal.ProposalStudents!.ToList(),
                extractedList: extractedStudents,
                keySelector: s => s.StudentCode!,
                isDifferent: (a, b) =>
                    a.FullName != b.FullName ||
                    a.Email != b.Email ||
                    a.Phone != b.Phone
            );

            // Reupload Student details
            await _studentService.ModifyManyAsync(studentTask, proposalId);

            var semesterResponse = await _semesterService.GetByIdAsync(semesterId);
            if (semesterResponse.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                await _s3.DeleteFile(uploadKey);
                return semesterResponse;
            }

            var semester = (semesterResponse.Data as SemesterDto)!;

            var proposalCode = await _historyService.GenerateProposalCodeAsync(semester.SemesterId
                , semester.SemesterCode, proposalId);
            if (proposalCode.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                await _s3.DeleteFile(fileKey);
                return proposalCode;
            }

            string proposalCodeValue = (proposalCode.Data as string)!;
            // Create new history
            var history = file.fileDetail is ProposalHistoryDto dto
                ? new ProposalHistoryDto
                {
                    Status = dto.Status,
                    Version = latestVersion + 1,
                    ProposalCode = proposalCodeValue,
                    Url = fileKey,
                    ProcessById = user.UserId,
                    ProcessDate = DateTime.UtcNow,
                    Comment = dto.Comment,
                    SimilarProposals = dto.SimilarProposals,
                    ProjectProposalId = proposalId
                }
                : new ProposalHistoryDto
                {
                    Status = ProjectProposalStatus.Pending.ToString(),
                    Version = latestVersion + 1,
                    Url = fileKey,
                    ProposalCode = proposalCodeValue,
                    ProcessById = user.UserId,
                    ProcessDate = DateTime.UtcNow,
                    ProjectProposalId = proposalId
                };

            await _historyService.CreateWithoutSaveAsync(history);
            // Update Proposal
            var updateResult = await _projectService.UpdateAsync(proposalId, projectProposal);

            if (updateResult.ResultCode != ResultCodeConst.SYS_Success0003)
            {
                await _s3.DeleteFile(fileKey);
                return updateResult;
            }
            // delete data in opensearch db

            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
        }
        catch (Exception ex)
        {
            if (uploadKey != string.Empty)
            {
                await _s3.DeleteFile(uploadKey);
            }

            return new ServiceResult(ResultCodeConst.Proposal_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Warning0001) + ": " + ex.Message);
        }
    }

    public async Task<IServiceResult> CheckDuplicatedProposal(List<IFormFile> files)
    {
        try
        {
            var extractedDocuments = await _extractService.ExtractDocuments(files);
            var documentEmbeddings =
                await _extractService.ExtractTexts(extractedDocuments.Select(x => x.Text).ToList());

            var tasks = extractedDocuments.Select(async (document, i) =>
            {
                var embeddings = documentEmbeddings[i].Select(e => e.Vector).ToList();


                var proposalMatches = new Dictionary<int, List<ProposalMatch>>();
                var chunkResults = await QueryChunks(embeddings);

                for (var j = 0; j < chunkResults.Count; j++)
                {
                    var uploadedChunkText = documentEmbeddings[i][j].Text;
                    foreach (var match in chunkResults[j])
                    {
                        if (match.Similarity < _appSettings.Threshold) continue;

                        if (!proposalMatches.TryGetValue(match.ProposalId, out var list))
                        {
                            list = new List<ProposalMatch>();
                            proposalMatches[match.ProposalId] = list;
                        }

                        list.Add(match with { OriginChunkId = j, UploadedChunkText = uploadedChunkText });
                    }
                }

                var totalChunks = embeddings.Count;
                var matchReports = proposalMatches.Select(entry =>
                {
                    var distinctChunks = new HashSet<int>(entry.Value.Select(m => m.OriginChunkId!.Value));
                    return new MatchReport(
                        ProposalId: entry.Key,
                        Name: entry.Value[0].Name,
                        // MatchCount: distinctChunks.Count,
                        MatchCount: entry.Value.Count,
                        LongestContiguous: CalcLongestContiguous(distinctChunks.ToList()),
                        MatchRatio: (double)distinctChunks.Count / totalChunks,
                        AvgSimilarity: entry.Value.Sum(m => m.Similarity) / entry.Value.Count,
                        Matches: entry.Value
                            // .DistinctBy(m => m.ChunkId)
                            .OrderByDescending(m => m.Similarity)
                            .ToList()
                    );
                }).ToList();

                return new ProposalAnalysisResult
                {
                    Name = document.Name,
                    Context = document.Context,
                    Solution = document.Solution,
                    Text = document.Text,
                    MatchedProposals = matchReports
                };
            });

            var resultList = await Task.WhenAll(tasks);

            return new ServiceResult(ResultCodeConst.Proposal_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0001))
            {
                Data = resultList
            };
        }
        catch (Exception e)
        {
            return new ServiceResult(ResultCodeConst.Proposal_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Success0001) + ": " + e.Message);
        }
    }

    public async Task<IServiceResult> UpdateStatus(int proposalId, bool isApproved, string email)
    {
        try
        {
            var userResponse = await _userService.GetCurrentUserAsync(email);
            if (userResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                return userResponse;

            var user = (userResponse.Data as UserDto)!;

            var proposalResponse = await _projectService.GetByIdAsync(proposalId);
            if (proposalResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                return proposalResponse;

            var proposal = proposalResponse.Data as ProjectProposalDto;

            if (proposal!.ApproverId.Equals(user.UserId))
            {
                return new ServiceResult(ResultCodeConst.Proposal_Warning0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Warning0003));
            }

            if (proposal.Status == ProjectProposalStatus.Approved)
            {
                return new ServiceResult(ResultCodeConst.Proposal_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Warning0004));
            }

            var updateResult = await _projectService.UpdateStatus(proposalId, isApproved);
            if (updateResult.ResultCode != ResultCodeConst.SYS_Success0003)
            {
                return updateResult;
            }

            if (isApproved)
            {
                var rawString = proposal.EngTitle + " " +
                                proposal.ContextText + " " +
                                proposal.SolutionText;
                await UploadChunks(new List<(int, string, string)>
                {
                    (proposal.ProjectProposalId, rawString, proposal.EngTitle)
                });
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress update status and upload to opensearch");
        }
    }

    public async Task<IServiceResult> SubmitReview<T>(int proposalId, T session, string email) where T : class
    {
        try
        {
            // Get user
            var user = await _userService.GetCurrentUserAsync(email);
            if (user.ResultCode != ResultCodeConst.SYS_Success0002)
                return user;
            var userDto = (user.Data as UserDto)!;
            var baseSpec = new BaseSpecification<ProjectProposal>(x => x.ProjectProposalId == proposalId);
            baseSpec.ApplyInclude(q => q.Include(p => p.ProposalHistories)
                .ThenInclude(h => h.ReviewSessions));
            var proposalResponse = await _projectService.GetWithSpecAsync(baseSpec);
            if (proposalResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                return proposalResponse;
            var proposal = (proposalResponse.Data as ProjectProposalDto)!;

            // // Get latest version
            // var historyResponse = await _historyService.GetLatestHistoryByProposalIdAsync(proposalId);
            // if (historyResponse.ResultCode != ResultCodeConst.SYS_Success0002)
            //     return historyResponse;
            // var history = (historyResponse.Data as ProposalHistoryDto)!;
            var history = proposal.ProposalHistories.MaxBy(h => h.Version);

            // Get Session to check if reviewer has this task or not
            var retrieveSession = await _reviewSessionService.IsReviewerTask(userDto.UserId, history!.HistoryId);

            if (retrieveSession.ResultCode != ResultCodeConst.SYS_Success0002)
                return retrieveSession;
            var sessionData = (retrieveSession.Data as ReviewSessionDto)!;

            ReviewSessionDto sessionInput = session as ReviewSessionDto ??
                                            throw new ArgumentException("Invalid session type");
            // Create List Answer
            sessionData.Answers = sessionInput.Answers;
            sessionData.ReviewDate = DateTime.Now;
            sessionData.Comment = sessionInput.Comment;
            sessionData.ReviewStatus = sessionInput.ReviewStatus;

            // Count the number of finished sessions to update proposal session
            var allSessions = await _reviewSessionService.GetByHistoryId(history.HistoryId);
            if (allSessions.ResultCode != ResultCodeConst.SYS_Success0002)
                return allSessions;

            var sessions = (allSessions.Data as List<ReviewSessionDto>)!;
            // Update status of session that still not change in db
            var existingHistory = proposal.ProposalHistories
                .FirstOrDefault(h => h.HistoryId == history.HistoryId);

            if (existingHistory != null)
            {
                proposal.ProposalHistories.Remove(existingHistory);
            }
            proposal.ProposalHistories.Add(history);
            
            var index = sessions.FindIndex(s => s.SessionId == sessionData.SessionId);
            sessions[index] = sessionData;

            if (sessions.Count(dto => dto.ReviewStatus.Equals(ReviewStatus.Approved)) >= 2)
            {
                proposal.Status = ProjectProposalStatus.Approved;
                var rawString = proposal.EngTitle + " " +
                                proposal.ContextText + " " +
                                proposal.SolutionText;
                await UploadChunks(new List<(int, string, string)>
                {
                    (proposal.ProjectProposalId, rawString, proposal.EngTitle)
                });
            }
            else if (sessions.Count(dto => dto.ReviewStatus.Equals(ReviewStatus.Rejected)) >= 2)
            {
                proposal.Status = ProjectProposalStatus.Rejected;
            }
            else if (!sessions.Any(dto => dto.ReviewStatus.Equals(ReviewStatus.Pending)))
            {
                proposal.Status = ProjectProposalStatus.Revised;
            }
            
            // Update all
            var updateSessionResult = await _projectService.UpdateAsync(proposalId, proposal);
            if (updateSessionResult.ResultCode != ResultCodeConst.SYS_Success0003)
            {
                return updateSessionResult;
            }

            return new ServiceResult(ResultCodeConst.Review_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Review_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress update status and upload to opensearch");
        }
    }

    public async Task<IServiceResult> GetFile(int historyId)
    {
        try
        {
            var historyResponse = await _historyService.GetById(historyId);
            if (historyResponse.ResultCode != ResultCodeConst.SYS_Success0002)
                return historyResponse;

            var history = (historyResponse.Data as ProposalHistoryDto)!;
            var s3File = await _s3.GetFile(history.Url);

            var resultData = ConvertS3ToStreamResult(s3File, history.Url);

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002))
            {
                Data = resultData
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return new ServiceResult(ResultCodeConst.Proposal_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Proposal_Warning0001) + ": " + ex.Message);
        }
    }

    private async Task<List<List<ProposalMatch>>> QueryChunks(List<List<double>> vectors, int k = 5)
    {
        var mSearchPayload = new StringBuilder();

        vectors.ForEach(vector =>
        {
            mSearchPayload
                .AppendLine(JsonSerializer.Serialize(new { index = "proposals" }))
                .AppendLine(JsonSerializer.Serialize(new
                {
                    size = k,
                    query = new
                    {
                        knn = new
                        {
                            vector_embedding = new { vector, k }
                        }
                    }
                }));
        });

        var body = PostData.String(mSearchPayload.ToString());

        var stringResponse = await _openSearchClient.MultiSearchAsync<StringResponse>(body);

        using var jsonDoc = JsonDocument.Parse(stringResponse.Body);

        return jsonDoc.RootElement
            .GetProperty("responses")
            .EnumerateArray()
            .Select((res, i) =>
                res.GetProperty("hits").GetProperty("hits").EnumerateArray()
                    .Select(hit =>
                    {
                        var source = hit.GetProperty("_source");
                        var vector = vectors[i];
                        var resVector = source.GetProperty("vector_embedding").EnumerateArray()
                            .Select(x => x.GetDouble()).ToList();

                        return new ProposalMatch(
                            source.GetProperty("proposal_id").GetInt32(),
                            source.GetProperty("name").GetString()!,
                            source.GetProperty("chunk_id").GetInt32(),
                            source.GetProperty("text").GetString()!,
                            hit.GetProperty("_score").GetDouble(),
                            CosineSimilarity(resVector, vector),
                            null, null
                        );
                    }).ToList()
            ).ToList();
    }

    private async Task UploadChunks(List<(int ProjectProposalId, string Text, string Name)> proposals)
    {
        var bulkPayload = new StringBuilder();
        var textChunks = await _extractService.ExtractTexts(proposals.Select(p => p.Text).ToList());
        for (var i = 0; i < proposals.Count; i++)
        {
            var proposal = proposals[i];
            var chunks = textChunks[i];
            foreach (var chunk in chunks)
            {
                var indexMeta = new
                {
                    index = new
                    {
                        _index = "proposals",
                        _id = $"{proposal.ProjectProposalId}_{chunk.ChunkId}"
                    }
                };

                var doc = new
                {
                    proposal_id = proposal.ProjectProposalId,
                    name = proposal.Name,
                    chunk_id = chunk.ChunkId,
                    text = chunk.Text,
                    vector_embedding = chunk.Vector
                };

                bulkPayload.AppendLine(JsonSerializer.Serialize(indexMeta));
                bulkPayload.AppendLine(JsonSerializer.Serialize(doc));
            }
        }

        // send request to OpenSearch
        var responseOpenSearch = await _openSearchClient.BulkAsync<StringResponse>(
            PostData.String(bulkPayload.ToString()),
            new BulkRequestParameters
            {
                Refresh = Refresh.True
            }
        );
        // check for error
        if (responseOpenSearch.HttpStatusCode is null or >= 400 ||
            responseOpenSearch.Body.Contains("\"errors\":true"))
        {
            throw new Exception("Errors while upload chunks: " + responseOpenSearch.Body);
        }
    }

    private int CalcLongestContiguous(List<int> ids)
    {
        var set = new HashSet<int>(ids);
        var best = 0;
        ids.ForEach(id =>
        {
            if (set.Contains(id - 1)) return;
            var length = 1;
            while (set.Contains(id + length)) length++;
            best = Math.Max(best, length);
        });

        return best;
    }

    private double CosineSimilarity(List<double> vectorA, List<double> vectorB)
    {
        if (vectorA.Count != vectorB.Count)
            throw new ArgumentException("Vectors must be of the same length");

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Count; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0 || normB == 0)
            return 0.0; // tránh chia cho 0

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private (Stream Stream, string ContentType, string FileName) ConvertS3ToStreamResult(GetObjectResponse s3File,
        string fileKey)
    {
        var memoryStream = new MemoryStream();
        s3File.ResponseStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var contentType = s3File.Headers.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            var extension = Path.GetExtension(fileKey).ToLower();
            contentType = extension switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        return (memoryStream, contentType, RemoveGuidPrefix(fileKey));
    }

    private Dictionary<string, List<T>> GetSyncPlan<T>(
        List<T> dtoList,
        List<T> extractedList,
        Func<T, string> keySelector,
        Func<T, T, bool> isDifferent)
    {
        var result = new Dictionary<string, List<T>>();

        var dbDict = dtoList.ToDictionary(keySelector);
        var extractedDict = extractedList.ToDictionary(keySelector);

        var toCreate = new List<T>();
        var toUpdate = new List<T>();
        var toDelete = new List<T>();

        foreach (var extracted in extractedList)
        {
            var key = keySelector(extracted);
            if (!dbDict.ContainsKey(key))
            {
                toCreate.Add(extracted);
            }
            else if (isDifferent(dbDict[key], extracted))
            {
                toUpdate.Add(extracted);
            }
        }

        foreach (var dbItem in dtoList)
        {
            var key = keySelector(dbItem);
            if (!extractedDict.ContainsKey(key))
            {
                toDelete.Add(dbItem);
            }
        }

        if (toCreate.Any()) result["Create"] = toCreate;
        if (toUpdate.Any()) result["Update"] = toUpdate;
        if (toDelete.Any()) result["Delete"] = toDelete;

        return result;
    }

    private string RemoveGuidPrefix(string fileName)
    {
        var parts = fileName.Split('_');
        if (Guid.TryParse(parts[0], out _))
        {
            return string.Join('_', parts.Skip(1));
        }

        return fileName;
    }
}