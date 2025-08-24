using System.Text.Json.Serialization;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Application.Dtos.Reviews;

public class ReviewSessionDto
{
    public int SessionId { get; set; }

    public Guid ReviewerId { get; set; }
    public int HistoryId { get; set; }

    public DateTime? ReviewDate { get; set; }
    public string? Comment { get; set; }    
    public ReviewStatus ReviewStatus { get; set; }
    
    public UserDto Reviewer { get; set; } = null!;

    [JsonIgnore]
    public ProposalHistoryDto History { get; set; } = null!;

    public ICollection<ReviewAnswerDto> Answers { get; set; } = new List<ReviewAnswerDto>();
}