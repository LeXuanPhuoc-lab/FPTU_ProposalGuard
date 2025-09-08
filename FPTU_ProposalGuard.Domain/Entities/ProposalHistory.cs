using System.Text.Json.Serialization;

namespace FPTU_ProposalGuard.Domain.Entities;

public class ProposalHistory
{
    public int HistoryId { get; set; }

    public int ProjectProposalId { get; set; }

    public string Status { get; set; } = null!;
    public int Version { get; set; } 

    public string ProposalCode { get; set; } = null!;
    public string Url { get; set; } = null!;
    public Guid ProcessById { get; set; }
    public string MD5Hash { get; set; } = null!;

    public DateTime ProcessDate { get; set; }

    public string? Comment { get; set; }

    public User ProcessBy { get; set; } = null!;
    
    [JsonIgnore]
    public ProjectProposal ProjectProposal { get; set; } = null!;
    [JsonIgnore]
    public ICollection<ProposalSimilarity> SimilarProposals { get; set; } = new List<ProposalSimilarity>();
    
    [JsonIgnore]
    public ICollection<ReviewSession> ReviewSessions { get; set; } = new List<ReviewSession>();

    [JsonIgnore]
    public ICollection<ReviewAnswer> ReviewAnswers { get; set; } = new List<ReviewAnswer>();
}
