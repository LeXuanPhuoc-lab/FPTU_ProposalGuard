using System.Text.Json.Serialization;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;

namespace FPTU_ProposalGuard.Domain;

public class ReviewAnswer
{
    public int AnswerId { get; set; }

    public int ReviewSessionId { get; set; }
    public int QuestionId { get; set; }
    public bool Answer { get; set; }

    [JsonIgnore]
    public ReviewSession ReviewSession { get; set; } = null!;

    [JsonIgnore]
    public ReviewQuestion Question { get; set; } = null!;

}