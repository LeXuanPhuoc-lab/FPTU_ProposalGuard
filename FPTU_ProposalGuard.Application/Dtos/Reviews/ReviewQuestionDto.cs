using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Application.Dtos.Reviews;

public class ReviewQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; }
    public AnswerType AnswerType { get; set; }

    public ICollection<ReviewAnswerDto> ReviewAnswers { get; set; } = new List<ReviewAnswerDto>();
}