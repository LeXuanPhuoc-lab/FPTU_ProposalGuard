using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

namespace FPTU_ProposalGuard.Domain.Interfaces;

public interface IReviewQuestionService<TDto> : IGenericService<ReviewQuestion, TDto, int>
    where TDto : class
{
}