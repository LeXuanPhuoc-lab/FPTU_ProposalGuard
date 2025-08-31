using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services;

public interface IUserService<TDto> : IGenericService<User, TDto, Guid>
    where TDto : class
{
    Task<IServiceResult> GetCurrentUserAsync(string email);
    Task<IServiceResult> UpdateProfileAsync(TDto dto);
    Task<IServiceResult> SignInAsync(string email);
    Task<IServiceResult> SignInWithPasswordAsync(string email, string pwd);
    Task<IServiceResult> SignInWithOtpAsync(string email, string otp);
    Task<IServiceResult> SignInWithGoogleAsync(string code);
    Task<IServiceResult> RefreshTokenAsync(string accessToken, string refreshTokenId);
    Task<IServiceResult> ChangePasswordAsync(string email, string password, string? token = null);
    Task<IServiceResult> VerifyChangePasswordOtpAsync(string email, string otp);
    Task<IServiceResult> ForgotPasswordAsync(string email);
    Task<IServiceResult> ResendOtpAsync(string email);
    Task<IServiceResult> EnableMfaAsync(string email);
    Task<IServiceResult> GetMfaBackupAsync(string email);
    Task<IServiceResult> ValidateMfaAsync(string email, string otp);
    Task<IServiceResult> ValidateMfaBackupCodeAsync(string email, string backupCode);
    Task<IServiceResult> RegenerateMfaBackupCodeAsync(string email);
    Task<IServiceResult> ConfirmRegenerateMfaBackupCodeAsync(string email, string otp, string token);
    Task<IServiceResult> SoftDeleteAsync(Guid userId);
    Task<IServiceResult> SoftDeleteRangeAsync(Guid[] userIds);
    Task<IServiceResult> UndoDeleteAsync(Guid userId);
    Task<IServiceResult> UndoDeleteRangeAsync(Guid[] userIds);
    Task<IServiceResult> DeleteRangeAsync(Guid[] userIds);
    Task<IServiceResult> CreateUnexistingUsersAsync(List<TDto> dtos);
}