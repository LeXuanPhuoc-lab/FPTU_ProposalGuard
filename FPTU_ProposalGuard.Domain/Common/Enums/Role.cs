using System.ComponentModel;

namespace FPTU_ProposalGuard.Domain.Common.Enums;

public enum Role
{
    // Admin
    [Description("Quản trị hệ thống")]
    Administration,
    // Lecturer
    [Description("Giảng viên")]
    Lecturer,
    // Reviewer
    [Description("Giảng viên thẩm định")]
    Reviewer,
    [Description("Người giám sát")]
    Moderator,
}