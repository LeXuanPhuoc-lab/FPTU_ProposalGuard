using System.Text.RegularExpressions;
using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Exceptions;
using FPTU_ProposalGuard.Application.Utils;
using FPTU_ProposalGuard.Domain;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using FPTU_ProposalGuard.Domain.Specifications;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ProposalGuard.Application.Services;

public class ProposalHistoryService(
    ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger logger,
    IGenericRepository<ProposalHistory, int> history)
    : GenericService<ProposalHistory, ProposalHistoryDto, int>(msgService, unitOfWork, mapper, logger),
        IProposalHistoryService<ProposalHistoryDto>
{
    private readonly IGenericRepository<ProposalHistory, int> _history = history;

    public async Task<IServiceResult> GetById(int id)
    {
        try
        {
            var baseSpec = new BaseSpecification<ProposalHistory>(x => x.HistoryId == id);
            // Include related entities
            baseSpec.ApplyInclude(q => q.Include(h => h.ProjectProposal)
                .ThenInclude(pp => pp.Submitter));

            var entity = await _unitOfWork.Repository<ProposalHistory, int>().GetWithSpecAsync(baseSpec);
            if (entity == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<ProposalHistoryDto>(entity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get data");
        }
    }

    public async Task CreateWithoutSaveAsync(ProposalHistoryDto dto)
    {
        await _unitOfWork.Repository<ProposalHistory, int>()
            .AddAsync(_mapper.Map<ProposalHistory>(dto));
    }

    public async Task<IServiceResult> AddReviewersAsync(List<(int id, ProposalHistoryDto dto)> input)
    {
        try
        {
            foreach (var (id, dto) in input)
            {
                // Retrieve by id
                var baseSpec = new BaseSpecification<ProposalHistory>(x => x.HistoryId == id);
                baseSpec.ApplyInclude(q => q
                    .Include(h => h.ReviewSessions)
                    .Include(h => h.ProjectProposal)
                        .ThenInclude(pp => pp.Approver)!);
                var existingEntity = await _unitOfWork.Repository<ProposalHistory, int>().GetWithSpecAsync(baseSpec);
                if (existingEntity == null)
                {
                    // Not found {0}
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, "lịch sử để tiến hành sửa đổi"));
                }

                var localConfig = new TypeAdapterConfig();
                localConfig.NewConfig<ReviewSessionDto, ReviewSession>()
                    .Ignore(dest => dest.Reviewer) 
                    .Ignore(dest => dest.History);  

                existingEntity.ReviewSessions ??= new List<ReviewSession>();

                var existingReviewerIds = existingEntity.ReviewSessions.Select(r => r.ReviewerId).ToHashSet();
                
                var mappedSessions = dto.ReviewSessions.Adapt<List<ReviewSession>>(localConfig);
                foreach (var newSession in mappedSessions)
                {
                    if (!existingReviewerIds.Contains(newSession.ReviewerId))
                    {
                        existingEntity.ReviewSessions.Add(new ReviewSession
                        {
                            HistoryId = existingEntity.HistoryId,
                            ReviewerId = newSession.ReviewerId,
                            ReviewStatus = newSession.ReviewStatus,
                            ReviewDate = newSession.ReviewDate
                        });
                    }
                }
                // Process update
                await _history.UpdateAsync(existingEntity);

                // Check if has changed or not
                if (!_history.HasChanges(existingEntity))
                {
                    // Mark as update success
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
                }
            }

            // Save DB
            if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }

            // Mark as failed to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add reviewers");
        }
    }

    public async Task<IServiceResult> GenerateProposalCodeAsync(int semesterId, string semesterCode,
        int? proposalId = null)
    {
        try
        {
            string returnCode = string.Empty;
            if (proposalId == null)
            {
                // Get all code in history
                var baseSpec = new BaseSpecification<ProposalHistory>(x => x.ProjectProposal.SemesterId == semesterId);
                baseSpec.ApplyInclude(q => q.Include(h => h.ProjectProposal));

                var existingEntities =
                    (await _unitOfWork.Repository<ProposalHistory, int>().GetAllWithSpecAsync(baseSpec)).ToList();
                var existingCodes = existingEntities
                    .Select(h => h.ProposalCode)
                    .Where(code => !string.IsNullOrEmpty(code))
                    .ToList();
                returnCode = GenerateProposalCode(existingCodes, semesterCode, null);
            }
            else
            {
                // Get all proposal history by proposalId
                var baseSpec = new BaseSpecification<ProposalHistory>(x => x.ProjectProposalId == proposalId);
                var existingEntities =
                    (await _unitOfWork.Repository<ProposalHistory, int>().GetAllWithSpecAsync(baseSpec)).ToList();
                var maxVersion = existingEntities.MaxBy(x => x.Version);
                // Generate new next proposalCode
                List<string> input = new List<string>();
                input.Add(maxVersion!.ProposalCode);
                returnCode = GenerateProposalCode(input, semesterCode, maxVersion.Version);
            }
            
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), returnCode);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add reviewers");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, ProposalHistoryDto dto)
    {
        try
        {
            // Retrieve notification by id
            var baseSpec = new BaseSpecification<ProposalHistory>(x => x.HistoryId == id);
            baseSpec.ApplyInclude(q => q.Include(h => h.ProjectProposal)
                .ThenInclude(pp => pp.Approver)!);
            var existingEntity = await _unitOfWork.Repository<ProposalHistory, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "lịch sử để tiến hành sửa đổi"));
            }

            // change props

            existingEntity.Status = dto.Status;
            existingEntity.ProjectProposal.Status = dto.Status switch
            {
                "Approved" => ProjectProposalStatus.Approved,
                "Rejected" => ProjectProposalStatus.Rejected,
                "Pending" => ProjectProposalStatus.Pending,
                _ => existingEntity.ProjectProposal.Status
            };
            existingEntity.ProjectProposal.ApproverId = dto.ProjectProposal.ApproverId;

            // Process update
            await _history.UpdateAsync(existingEntity);

            // Check if has changed or not
            if (!_history.HasChanges(existingEntity))
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }

            // Save DB
            if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }

            // Mark as failed to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update History");
        }
    }

    public async Task<IServiceResult> GetLatestHistoryByProposalIdAsync(int proposalId)
    {
        try
        {
            var baseSpec = new BaseSpecification<ProposalHistory>(h => h.ProjectProposalId == proposalId);
            baseSpec.ApplyInclude(q => q.Include(ph => ph.ReviewSessions));

            var latestHistory = (await _unitOfWork.Repository<ProposalHistory, int>()
                .GetAllWithSpecAsync(baseSpec)).MaxBy(ph => ph.Version);
            if (latestHistory == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "Không có lịch sử của đề xuất này"));
            }
            var historyDto = _mapper.Map<ProposalHistoryDto>(latestHistory);
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), historyDto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update History");
        }
    }

    public async Task<IServiceResult> UpdateHistoryReview(int id, ProposalHistoryDto dto, string proposalStatus)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();    
        try
        {
            var baseSpec = new BaseSpecification<ProposalHistory>(ph => ph.HistoryId == id);
            baseSpec.ApplyInclude(q => q.Include(h => h.ProjectProposal)
                .Include(h => h.ReviewSessions)
                .ThenInclude(s=> s.Answers));
            var existingEntity = await _unitOfWork.Repository<ProposalHistory, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "lịch sử để tiến hành sửa đổi"));
            }
            // Update review sessions
            existingEntity.ProjectProposal.Status = proposalStatus switch
            {
                "Approved" => ProjectProposalStatus.Approved,
                "Rejected" => ProjectProposalStatus.Rejected,
                "Pending" => ProjectProposalStatus.Pending,
                "Revised" => ProjectProposalStatus.Revised,
                _ => existingEntity.ProjectProposal.Status
            };
            // _mapper.Map(dto, existingEntity);
            
            dto.Adapt(existingEntity);
            foreach (var existingEntityReviewSession in existingEntity.ReviewSessions)
            {
                // Find the corresponding review session in dto
                var reviewSessionDto = dto.ReviewSessions.FirstOrDefault(rs => rs.SessionId == existingEntityReviewSession.SessionId);
                if (reviewSessionDto != null)
                {
                    // Update existing review session with dto values
                    existingEntityReviewSession.ReviewDate = reviewSessionDto.ReviewDate;
                    existingEntityReviewSession.Comment = reviewSessionDto.Comment;
                    existingEntityReviewSession.ReviewStatus = reviewSessionDto.ReviewStatus;
                    existingEntityReviewSession.Answers = _mapper.Map<List<ReviewAnswer>>(reviewSessionDto.Answers);
                }
            }
            
            // Process update
            await _unitOfWork.Repository<ProposalHistory, int>().UpdateAsync(existingEntity);
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<ProposalHistory, int>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)    
        {
            _logger.Error(ex.Message);
            throw;
        }
        return serviceResult;
    }

    private string GenerateProposalCode(List<string> codes, string semesterCode, int? version)
    {
        if (codes is null || codes.Count == 0)
        {
            return $"{semesterCode}001";
        }

        if (version != null)
        {
            string baseCode = Regex.Replace(codes[0], "_RE\\d+$", ""); 
            
            string versionSuffix = $"_RE{version.Value:D2}";

            return $"{baseCode}{versionSuffix}";
        }
        else
        {
            string prefix = Regex.Escape(semesterCode);
            var latestIndex = codes.Select(
                    code =>
                    {
                        //Remove Reup postfix
                        string cleaned = Regex.Replace(code, "_RE\\d+$", "");
                        var match = Regex.Match(cleaned, $"{prefix}(\\d+)$");
                        if (match.Success)
                        {
                            return int.Parse(match.Groups[1].Value);
                        }

                        return -1;
                    })
                .Where(n => n != -1)
                .DefaultIfEmpty(-1)
                .Max();
            if (latestIndex == -1)
            {
                return $"{semesterCode}001";
            }

            int newIndex = latestIndex + 1;
            return $"{semesterCode}{newIndex:D3}";
        }
    }
}