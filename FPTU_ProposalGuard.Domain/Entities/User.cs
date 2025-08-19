namespace FPTU_ProposalGuard.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? Dob { get; set; }

    public string? Phone { get; set; }

    public string? Avatar { get; set; }
    
    public string? Address { get; set; }

    public string? Gender { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? TwoFactorSecretKey { get; set; }

    public string? TwoFactorBackupCodes { get; set; }

    public string? PhoneVerificationCode { get; set; }
    public string? EmailVerificationCode { get; set; }
    public DateTime? PhoneVerificationExpiry { get; set; }

    public int RoleId { get; set; }

    public ICollection<Notification> NotificationCreations { get; set; } = new List<Notification>();
    
    public ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();

    public ICollection<ProjectProposal> ProjectProposalApprovers { get; set; } = new List<ProjectProposal>();

    public ICollection<ProjectProposal> ProjectProposalSubmitters { get; set; } = new List<ProjectProposal>();

    public ICollection<ProposalHistory> ProposalHistories { get; set; } = new List<ProposalHistory>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ReviewSession> ReviewSessions { get; set; } = new List<ReviewSession>();

    public SystemRole Role { get; set; } = null!;
}
