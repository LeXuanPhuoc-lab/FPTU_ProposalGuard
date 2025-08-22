using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.Users;
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
    IUserService<UserDto> userService,
    ILogger logger)
    : GenericService<ReviewSession, ReviewSessionDto, int>(msgService, unitOfWork, mapper, logger),
        IReviewSessionService<ReviewSessionDto>
{
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