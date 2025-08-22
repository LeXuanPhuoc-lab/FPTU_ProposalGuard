using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Application.Dtos.Reviews;

public class ReviewSessionToBeReviewed
{
    public int SessionId { get; set; }

    public Guid ReviewerId { get; set; }
    
    public int HistoryId { get; set; }

    public DateTime? ReviewDate { get; set; }

    public ReviewStatus ReviewStatus { get; set; }

    public ProjectProposalDto Proposal { get; set; } = null!;
}