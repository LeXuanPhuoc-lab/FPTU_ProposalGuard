namespace FPTU_ProposalGuard.API.Payloads.Requests.Proposals;

public class SubmitReviewRequest
{
    public List<SingleAnswer> SingleAnswers { get; set; } = new List<SingleAnswer>();
    public string? Comment { get; set; } = null!;
    public string ReviewStatus { get; set; }
}

public class SingleAnswer
{
    public int QuestionId { get; set; }
    public bool Answer { get; set; }
}
