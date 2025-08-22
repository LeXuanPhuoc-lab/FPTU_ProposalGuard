using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Application.Dtos.Semesters;

public class SemesterDto
{
    public int SemesterId { get; set; }

    public string SemesterCode { get; set; } = null!;

    public int Year { get; set; }

    public Term Term { get; set; }
}