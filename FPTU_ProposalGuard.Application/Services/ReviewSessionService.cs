using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.Users;
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

public class ReviewSessionService(
    ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IGenericRepository<ReviewSession, int> reviewSessionRepository,
    IUserService<UserDto> userService,
    ILogger logger)
    : GenericService<ReviewSession, ReviewSessionDto, int>(msgService, unitOfWork, mapper, logger),
        IReviewSessionService<ReviewSessionDto>
{
    public IGenericRepository<ReviewSession, int> ReviewSessionRepository { get; } = reviewSessionRepository;

    public async Task<IServiceResult> IsReviewerTask(Guid userId, int historyId)
    {
        try
        {
            var reviewSession = new BaseSpecification<ReviewSession>(rs =>
                rs.HistoryId == historyId && rs.ReviewerId.Equals(userId));
            var reviewSessions = await _unitOfWork.Repository<ReviewSession,int>().GetWithSpecAsync(reviewSession);
            if (reviewSessions == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<ReviewSessionDto>(reviewSessions));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }
    }

    public async Task<IServiceResult> GetByHistoryId(int historyId)
    {
        try
        {
            var reviewSessionSpec = new BaseSpecification<ReviewSession>(rs => rs.HistoryId == historyId);
            var reviewSessions =
                await _unitOfWork.Repository<ReviewSession, int>().GetAllWithSpecAsync(reviewSessionSpec);
            if (reviewSessions == null || !reviewSessions.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<List<ReviewSessionDto>>(reviewSessions));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }
    }

    public async Task<IServiceResult> UpdateSubmitSession(int sessionId, ReviewSessionDto sessionDetail, string proposalStatus)
    {
        var serviceResult = new ServiceResult();
        try
        {
            var baseSpec = new BaseSpecification<ReviewSession>(x => x.SessionId == sessionId);
            // Apply include
            baseSpec.ApplyInclude(q => 
                q.Include(s => s.History)
                    .ThenInclude(h => h.ProjectProposal)
                .Include(s => s.Answers));
            var session = await _unitOfWork.Repository<ReviewSession, int>().GetWithSpecAsync(baseSpec);
            if (session == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002));
            }
            session.Answers = _mapper.Map<List<ReviewAnswer>>(sessionDetail.Answers);
            session.ReviewStatus = sessionDetail.ReviewStatus;
            session.ReviewDate = DateTime.UtcNow;
            session.Comment = sessionDetail.Comment;
            
            session.History.ProjectProposal.Status = proposalStatus switch
            {
                "Approved" => ProjectProposalStatus.Approved,
                "Rejected" => ProjectProposalStatus.Rejected,
                "Revised" => ProjectProposalStatus.Revised,
                _ => ProjectProposalStatus.Pending
            };

            session.History.Status = proposalStatus;

            await _unitOfWork.Repository<ReviewSession, int>().UpdateAsync(session);
            if (!_unitOfWork.Repository<ReviewSession, int>().HasChanges(session))
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
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return serviceResult;
    }

    public async Task<IServiceResult> GetSessionsToBeReviewed(string email)
    {
        var userResponse = await userService.GetCurrentUserAsync(email);
        if (userResponse.ResultCode != ResultCodeConst.SYS_Success0002)
            return userResponse;
        
        var userId = (userResponse.Data as UserDto)!.UserId;

        var baseSpec = new BaseSpecification<ReviewSession>((rs) =>
                rs.ReviewerId.Equals(userId)
            && rs.ReviewStatus.Equals(ReviewStatus.Pending)
            && !rs.History.ProjectProposal.Status.Equals(ReviewStatus.Revised)
        );

        baseSpec.ApplyInclude(rs =>
            rs.Include(rsi => rsi.History)
                .ThenInclude(h => h.ProjectProposal));

        var reviewSessions = await unitOfWork.Repository<ReviewSession, int>().GetAllWithSpecAsync(baseSpec);

        var reviewSessionToBeReviewed = reviewSessions!.Select(rs => new ReviewSessionToBeReviewed()
        {
            HistoryId = rs.HistoryId,
            ReviewerId = rs.ReviewerId,
            ReviewStatus = rs.ReviewStatus,
            ReviewDate = rs.ReviewDate,
            SessionId = rs.SessionId,
            Proposal = rs.History.ProjectProposal.Adapt<ProjectProposalDto>()
        });

        // Return with count number
        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            reviewSessionToBeReviewed);
    }
}