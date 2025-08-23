using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using FPTU_ProposalGuard.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ProposalGuard.Application.Services;

public class ReviewSessionService(
    ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IGenericRepository<ReviewSession, int> reviewSessionRepository,
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
}