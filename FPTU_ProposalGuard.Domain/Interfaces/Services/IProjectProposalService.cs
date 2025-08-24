using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using FPTU_ProposalGuard.Domain.Specifications.Interfaces;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services;

public interface IProjectProposalService <TDto> : IGenericService<ProjectProposal, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateManyAsync(List<TDto> dtos);
    Task<IServiceResult> GetByIdAsync(int id);
    Task<IServiceResult> UpdateAsync( int id,TDto dto);
    Task<IServiceResult> UpdateStatus(int id, bool isApproved);
    Task<IServiceResult> ExportSemesterReport(int? semesterId);
    Task<IServiceResult> UpdateReviewedProposal(int id, TDto dto);
}