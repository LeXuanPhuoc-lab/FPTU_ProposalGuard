namespace FPTU_ProposalGuard.Application.Dtos.Proposals;

public class ExistedReviewerDto
{
    public int ProposalId { get; set; }
    public List<string> ExistedEmails { get; set; } = new List<string>();
}