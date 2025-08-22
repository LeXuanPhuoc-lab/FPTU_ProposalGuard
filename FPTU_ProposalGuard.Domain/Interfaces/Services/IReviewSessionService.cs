using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services;

public interface IReviewSessionService <TDto> : IGenericService<ReviewSession, TDto, int>
    where TDto : class
{
    Task <IServiceResult> GetSessionsToBeReviewed(string email);
}