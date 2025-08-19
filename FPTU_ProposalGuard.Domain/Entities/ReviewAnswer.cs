using System.Text.Json.Serialization;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;

namespace FPTU_ProposalGuard.Domain;

public class ReviewAnswer
{
    public int AnswerId { get; set; }

    public int ReviewSessionId { get; set; }
    public int QuestionId { get; set; }
    public int HistoryId { get; set; }

    public string Answer { get; set; } = null!;

    [JsonIgnore]
    public ReviewSession ReviewSession { get; set; } = null!;

    [JsonIgnore]
    public ReviewQuestion Question { get; set; } = null!;

    [JsonIgnore]
    public ProposalHistory History { get; set; } = null!;
}