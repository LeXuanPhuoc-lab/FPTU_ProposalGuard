namespace FPTU_ProposalGuard.Application.Configurations;

public class CheckProposalSettings
{
    public required string ExtractTextApiKey { get; init; }
    public required string ExtractDocumentApiKey { get; init; }
    public required string ExtractDocumentUrl { get; init; }
    public required string ExtractTextUrl { get; init; }
    public double Threshold { get; init; }
}