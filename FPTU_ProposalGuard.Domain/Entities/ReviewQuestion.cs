using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Domain.Entities;

public class ReviewQuestion
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; }
    public AnswerType AnswerType { get; set; }

    public ICollection<ReviewAnswer> ReviewAnswers { get; set; } = new List<ReviewAnswer>();
    
}