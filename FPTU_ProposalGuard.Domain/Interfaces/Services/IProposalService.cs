using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services;

public interface IProposalService
{
    Task<IServiceResult> AddProposalsWithFiles<T>(List<(IFormFile file,T fileDetail)> files,int semesterId, string email) where T : class; 
    Task<IServiceResult> ReUploadProposal<T>((IFormFile file,T fileDetail) file,int proposalId, string email,int semesterId) where T : class;
    // Task<IServiceResult> AddProposals(List<(string Name,string Context, string Solution, string Text)> contexts
    // ,int semesterId, string email);
    Task<IServiceResult> CheckDuplicatedProposal(List<IFormFile> files);
    Task<IServiceResult> UpdateStatus(int proposalId,bool isApproved, string email);
    Task<IServiceResult> GetFile(int historyId);
    Task<IServiceResult> AddReviewers(IDictionary<int, List<string>> proposalReviewers);
    Task<IServiceResult> SubmitReview<T>(int proposalId, T session, string email) where T : class;


}
