using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Interfaces;

namespace FPTU_ProposalGuard.Domain.Entities;

public class ProjectProposal : IAuditableEntity
{
    public int ProjectProposalId { get; set; }

    public int SemesterId { get; set; }

    public string? VieTitle { get; set; }

    public string EngTitle { get; set; } = null!;

    public string? Abbreviation { get; set; }

    public string? SupervisorName { get; set; }

    public DateOnly DurationFrom { get; set; }

    public DateOnly DurationTo { get; set; }

    public string? Profession { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? KindsOfPerson { get; set; }

    public string ContextText { get; set; } = null!;

    public string SolutionText { get; set; } = null!;

    public string? FunctionalRequirements { get; set; }
    public string? NonFunctionalRequirements { get; set; }
    public string? TechnicalStack { get; set; }
    public string? Tasks { get; set; }
    public string? MainProposalContent { get; set; }

    public ProjectProposalStatus Status { get; set; }

    public Guid SubmitterId { get; set; }

    public Guid? ApproverId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public User? Approver { get; set; }

    public ICollection<ProposalHistory> ProposalHistories { get; set; } = new List<ProposalHistory>();

    public ICollection<ProposalSimilarity> ProposalSimilarityCheckedProposals { get; set; } = new List<ProposalSimilarity>();

    public ICollection<ProposalSimilarity> ProposalSimilarityExistingProposals { get; set; } = new List<ProposalSimilarity>();

    public ICollection<ProposalStudent>? ProposalStudents { get; set; } = new List<ProposalStudent>();

    public ICollection<ProposalSupervisor>? ProposalSupervisors { get; set; } = new List<ProposalSupervisor>();
    
    public Semester Semester { get; set; } = null!;

    public User Submitter { get; set; } = null!;
}
