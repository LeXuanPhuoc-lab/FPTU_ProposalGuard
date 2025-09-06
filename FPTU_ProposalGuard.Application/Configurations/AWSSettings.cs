namespace FPTU_ProposalGuard.Application.Configurations;

public class AWSSettings
{
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string Region { get; init; }
    public required string BucketName { get; init; }
    public required string OpenSearchUrl { get; init; }
    public required string OpenSearchUsername { get; init; }
    public required string OpenSearchPassword { get; init; }
}