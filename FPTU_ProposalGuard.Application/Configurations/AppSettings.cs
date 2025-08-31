namespace FPTU_ProposalGuard.Application.Configurations;

public class AppSettings
{
    public int PageSize { get; set; }
    public string AESKey { get; set; } = null!;
    public string AESIV { get; set; } = null!;
    public string HomeLink { get; set; } = null!;
}