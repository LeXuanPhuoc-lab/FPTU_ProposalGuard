namespace FPTU_ProposalGuard.API.Payloads.Requests.Proposals;

public class AddReviewersRequest
{
    public List<ProposalWithReviewer> Proposals { get; set; } = new List<ProposalWithReviewer>();
}

public class ProposalWithReviewer
{
    public int ProposalId { get; set; }
    public List<string> ReviewerEmails { get; set; } = new List<string>();
}