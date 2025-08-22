using System.Text.Json.Serialization;
using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Domain.Entities;

public class ReviewSession
{
    public int SessionId { get; set; }

    public Guid ReviewerId { get; set; }
    public int HistoryId { get; set; }

    public DateTime? ReviewDate { get; set; } = null;

    public ReviewStatus ReviewStatus { get; set; }

    [JsonIgnore] public User Reviewer { get; set; } = null!;

    [JsonIgnore] public ProposalHistory History { get; set; } = null!;

    public ICollection<ReviewAnswer> Answers { get; set; } = new List<ReviewAnswer>();
}