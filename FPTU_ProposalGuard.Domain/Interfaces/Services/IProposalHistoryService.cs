using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services;

public interface IProposalHistoryService<TDto> : IGenericService<ProposalHistory, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetById(int id);
    Task CreateWithoutSaveAsync(TDto dto);
    Task<IServiceResult> AddReviewersAsync(List<(int id, TDto dto)> input);
    Task<IServiceResult> GenerateProposalCodeAsync(int semesterId,string semesterCode,int? proposalId = null);
    Task<IServiceResult> GetLatestHistoryByProposalIdAsync(int proposalId);
}