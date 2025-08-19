using System.ComponentModel;

namespace FPTU_ProposalGuard.Domain.Common.Enums;

public enum AnswerType
{
    [Description("Đúng sai")]
    YesNo,
    [Description("Trả lời ngắn")]
    ShortAnswer,
}