using FPTU_ProposalGuard.Application.Dtos.Notifications;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.SystemRoles;
using System.Text.Json.Serialization;


namespace FPTU_ProposalGuard.Application.Dtos.Users;

public class UserDto
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

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
    public string? EmailVerificationCode { get; set; }

    public string? TwoFactorSecretKey { get; set; }

    public string? TwoFactorBackupCodes { get; set; }

    public string? PhoneVerificationCode { get; set; }

    public DateTime? PhoneVerificationExpiry { get; set; }

    public int RoleId { get; set; }

    public SystemRoleDto Role { get; set; } = null!;

    public ICollection<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();

    [JsonIgnore] public ICollection<ReviewSessionDto> ReviewSessions { get; set; } = new List<ReviewSessionDto>();

    #region Create dto when implement specific feature

    // public ICollection<ProjectProposal> ProjectProposalApprovers { get; set; } = new List<ProjectProposal>();
    //
    // public ICollection<ProjectProposal> ProjectProposalSubmitters { get; set; } = new List<ProjectProposal>();
    //
    // public ICollection<ProposalHistory> ProposalHistories { get; set; } = new List<ProposalHistory>();
    //
    // public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    #endregion
}