using System.Text.Json.Serialization;
using FPTU_ProposalGuard.Application.Dtos.Proposals;

namespace FPTU_ProposalGuard.Application.Dtos.Reviews;

public class ReviewAnswerDto
{
    public int AnswerId { get; set; }

    public int ReviewSessionId { get; set; }
    public int QuestionId { get; set; }
    
    public bool Answer { get; set; }

    [JsonIgnore]
    public ReviewSessionDto ReviewSession { get; set; } = null!;

    [JsonIgnore]
    public ReviewQuestionDto Question { get; set; } = null!;
}