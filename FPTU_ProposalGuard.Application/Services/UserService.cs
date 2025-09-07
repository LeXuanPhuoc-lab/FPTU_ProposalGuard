using System.IdentityModel.Tokens.Jwt;
using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Configurations;
using FPTU_ProposalGuard.Application.Dtos;
using FPTU_ProposalGuard.Application.Dtos.Authentications;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.SystemRoles;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Application.Exceptions;
using FPTU_ProposalGuard.Application.Extensions;
using FPTU_ProposalGuard.Application.Services.IExternalServices;
using FPTU_ProposalGuard.Application.Utils;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using FPTU_ProposalGuard.Domain.Specifications;
using FPTU_ProposalGuard.Domain.Specifications.Interfaces;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util;
using Mapster;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ProposalGuard.Application.Services;

public class UserService : GenericService<User, UserDto, Guid>, IUserService<UserDto>
{
    private readonly IGenericRepository<User, Guid> _userRepository;
    private readonly IGenericRepository<SystemRole, int> _roleRepository;

    private readonly AppSettings _appSettings;
    private readonly WebTokenSettings _webTokenSettings;
    private readonly GoogleAuthSettings _googleAuthSettings;
    private readonly TokenValidationParameters _tokenValidationParams;

    private readonly HttpClient _httpClient;
    private readonly IEmailService _emailService;
    private readonly IRefreshTokenService<RefreshTokenDto> _refreshTokenService;
    private readonly IProposalHistoryService<ProposalHistoryDto> _historyService;
    private readonly ISystemRoleService<SystemRoleDto> _roleService;

    public UserService(
        HttpClient httpClient,
        IEmailService emailService,
        ISystemMessageService msgService,
        ISystemRoleService<SystemRoleDto> roleService,
        IRefreshTokenService<RefreshTokenDto> refreshTokenService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger,
        IProposalHistoryService<ProposalHistoryDto> historyService,
        TokenValidationParameters tokenValidationParams,
        IOptionsMonitor<WebTokenSettings> monitor,
        IOptionsMonitor<GoogleAuthSettings> monitor1,
        IOptionsMonitor<AppSettings> monitor3)
        : base(msgService, unitOfWork, mapper, logger)
    {
        _userRepository = unitOfWork.Repository<User, Guid>();
        _roleRepository = unitOfWork.Repository<SystemRole, int>();

        _httpClient = httpClient;
        _appSettings = monitor3.CurrentValue;
        _webTokenSettings = monitor.CurrentValue;
        _googleAuthSettings = monitor1.CurrentValue;
        _tokenValidationParams = tokenValidationParams;

        _emailService = emailService;
        _roleService = roleService;
        _refreshTokenService = refreshTokenService;
        _historyService = historyService;
    }

    public override async Task<IServiceResult> CreateAsync(UserDto dto)
    {
        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Check role existence
            var roleEntity = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (roleEntity == null)
            {
                // Not found
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errMsg, "vai trò"));
            }

            // Add new entity
            await _userRepository.AddAsync(_mapper.Map<User>(dto));
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Create successfully
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0001,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001),
                    data: true);
            }

            // Failed to create
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Fail0001,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001),
                data: false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when process create user");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(Guid id, UserDto dto)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid validations", errors);
            }

            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, typeof(User).ToString().ToLower()));
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Update properties
            existingEntity.FirstName = dto.FirstName ?? string.Empty;
            existingEntity.LastName = dto.LastName ?? string.Empty;
            existingEntity.Dob = dto.Dob;
            existingEntity.Phone = dto.Phone;
            existingEntity.Address = dto.Address;
            existingEntity.Gender = dto.Gender;
            existingEntity.ModifiedDate = currentLocalDateTime;

            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<User, Guid>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Progress update when all require passed
            await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update user");
        }

        return serviceResult;
    }

    public override async Task<IServiceResult> GetByIdAsync(Guid id)
    {
        // Build spec
        var baseSpec = new BaseSpecification<User>(u => u.UserId.Equals(id));
        // Apply include
        baseSpec.ApplyInclude(u => u.Include(u => u.Role));
        // Get user by query specification
        var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
        if (existedUser is null)
        {
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }

        // Define a local Mapster configuration
        var localConfig = new TypeAdapterConfig();
        localConfig.NewConfig<User, UserDto>()
            .Ignore(dest => dest.PasswordHash!)
            .Ignore(dest => dest.RoleId)
            .Ignore(dest => dest.EmailConfirmed)
            .Ignore(dest => dest.TwoFactorEnabled)
            .Ignore(dest => dest.PhoneNumberConfirmed)
            .Ignore(dest => dest.TwoFactorSecretKey!)
            .Ignore(dest => dest.TwoFactorBackupCodes!)
            .Ignore(dest => dest.PhoneVerificationCode!)
            .Ignore(dest => dest.EmailVerificationCode!)
            .Ignore(dest => dest.PhoneVerificationExpiry!)
            .Map(dto => dto.Role, src => src.Role)
            .AfterMapping((src, dest) => { dest.Role.RoleId = 0; });

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            existedUser.Adapt<UserDto>(localConfig));
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<User> spec, bool tracked = true)
    {
        try
        {
            // Try to parse specification to UserSpecification
            var userSpec = spec as UserSpecification;
            // Check if specification is null
            if (userSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Define a local Mapster configuration
            var localConfig = new TypeAdapterConfig();
            localConfig.NewConfig<User, UserDto>()
                .Ignore(dest => dest.PasswordHash!)
                .Ignore(dest => dest.EmailConfirmed)
                .Ignore(dest => dest.TwoFactorEnabled)
                .Ignore(dest => dest.PhoneNumberConfirmed)
                .Ignore(dest => dest.TwoFactorSecretKey!)
                .Ignore(dest => dest.TwoFactorBackupCodes!)
                .Ignore(dest => dest.PhoneVerificationCode!)
                .Ignore(dest => dest.EmailVerificationCode!)
                .Ignore(dest => dest.PhoneVerificationExpiry!);

            // Count total users
            var totalUserWithSpec = await _unitOfWork.Repository<User, Guid>().CountAsync(userSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalUserWithSpec / userSpec.PageSize);

            // Set pagination to specification after count total users 
            if (userSpec.PageIndex > totalPage
                || userSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                userSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            userSpec.ApplyPaging(
                skip: userSpec.PageSize * (userSpec.PageIndex - 1),
                take: userSpec.PageSize);

            var entities = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(spec, tracked);
            if (entities.Any())
            {
                // Convert to dto collection 
                var userDtos = entities.Adapt<IEnumerable<UserDto>>(localConfig);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<UserDto>(userDtos,
                    userSpec.PageIndex, userSpec.PageSize, totalPage, totalUserWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                entities.Adapt<IEnumerable<UserDto>>(localConfig));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all data");
        }
    }

    public async Task<IServiceResult> GetCurrentUserAsync(string email)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Define a local Mapster configuration
            var localConfig = new TypeAdapterConfig();
            localConfig.NewConfig<User, UserDto>()
                .Ignore(dest => dest.PasswordHash!)
                .Ignore(dest => dest.EmailConfirmed)
                .Ignore(dest => dest.TwoFactorEnabled)
                .Ignore(dest => dest.PhoneNumberConfirmed)
                .Ignore(dest => dest.TwoFactorSecretKey!)
                .Ignore(dest => dest.TwoFactorBackupCodes!)
                .Ignore(dest => dest.PhoneVerificationCode!)
                .Ignore(dest => dest.EmailVerificationCode!)
                .Ignore(dest => dest.PhoneVerificationExpiry!);

            // Response
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: user.Adapt<UserDto>(localConfig));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get current user");
        }
    }

    public async Task<IServiceResult> UpdateProfileAsync(UserDto dto)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid validations", errors);
            }

            // Retrieve the entity
            var existingEntity = await _userRepository.GetWithSpecAsync(
                new BaseSpecification<User>(e => e.Email == dto.Email));
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "người dùng"));
            }

            // Update specific properties
            existingEntity.FirstName = dto.FirstName;
            existingEntity.LastName = dto.LastName;
            existingEntity.Dob = dto.Dob;
            existingEntity.Phone = dto.Phone;
            existingEntity.Address = dto.Address;
            existingEntity.Gender = dto.Gender;
            existingEntity.Avatar = dto.Avatar;

            // Check if there are any differences between the original and the updated entity
            if (!_userRepository.HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Progress update when all require passed
            await _userRepository.UpdateAsync(existingEntity);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
                return serviceResult;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }

        return serviceResult;
    }

    public async Task<IServiceResult> SignInAsync(string email)
    {
        try
        {
            bool isAdmin = false;

            // Retrieve user information
            var userSpec = new BaseSpecification<User>(x => x.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "email"));
            }

            // Check whether user is Admin
            if (user.Role.RoleName.Equals(nameof(Role.Administration))) isAdmin = true;

            // Check whether account has not updated password yet
            var allowSkipAuthRequired = (
                // Allow skip required when account password is empty and must be employee or admin
                string.IsNullOrEmpty(user.PasswordHash) && isAdmin);

            // Check email confirmation
            if (!user.EmailConfirmed && !allowSkipAuthRequired)
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0008));
            }

            // User is not yet in-active or not in deleted status
            if ((!user.IsActive || user.IsDeleted) && !allowSkipAuthRequired)
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
            }

            // Response to keep on sign-in with username/password
            if (!string.IsNullOrEmpty(user.PasswordHash)) // Exist password
            {
                return new ServiceResult(ResultCodeConst.Auth_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0003));
            }
            // Response to keep on sign-in with OTP
            // since user sign-up with external provider
            else
            {
                // Progress sending OTP to user's email
                // Generate confirmation code
                var otpCode = StringUtils.GenerateUniqueCode();
                // Email subject
                var emailSubject = "[ProposalGuard] Xác nhận đăng nhập";
                // Email content
                var emailContent = $@"
						<div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
						    <h3>Chào {user.FirstName} {user.LastName},</h3>
						    <p>Đây là mã xác nhận của bạn:</p>
						    <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
						    <p>Vui lòng sử dụng mã này để hoàn tất quá trình đăng nhập.</p>
						    <br />
						    <p>Cảm ơn,</p>
						    <p>ProposalGuard</p>
						</div>";

                // Process send and save OTP
                var isOtpSent = await SendAndSaveOtpAsync(otpCode, user, emailSubject, emailContent);
                if (isOtpSent) // Email sent
                {
                    return new ServiceResult(ResultCodeConst.Auth_Success0005,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005));
                }

                // Fail to send email
                return new ServiceResult(ResultCodeConst.Auth_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when progress sign-in");
        }
    }

    public async Task<IServiceResult> SignInWithPasswordAsync(string email, string pwd)
    {
        try
        {
            // Retrieve user information
            var userSpec = new BaseSpecification<User>(x => x.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "email"));
            }

            // Validate password
            if (ValidatePassword(pwd, user.PasswordHash))
            {
                if (user.TwoFactorEnabled) // Is enable MFA account
                {
                    return new ServiceResult(ResultCodeConst.Auth_Warning0010,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010));
                }

                // Handle authenticate user
                return await AuthenticateUserAsync(_mapper.Map<UserDto>(user));
            }
            else // Password not match
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0007));
            }
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when progress sign-in");
        }
    }

    public async Task<IServiceResult> SignInWithOtpAsync(string email, string otp)
    {
        try
        {
            // Retrieve user information
            var userSpec = new BaseSpecification<User>(x => x.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "email"));
            }

            // Check match confirmation code
            if (user.EmailVerificationCode == otp) // Match
            {
                // Handle authenticate user
                return await AuthenticateUserAsync(_mapper.Map<UserDto>(user));
            }

            // Msg: OTP code is incorrect, please resend
            return new ServiceResult(ResultCodeConst.Auth_Warning0005,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress sign-in");
        }
    }

    public async Task<IServiceResult> SignInWithGoogleAsync(string code)
    {
        try
        {
            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForAccessTokenAsync(code);

            // Validate the access token
            GoogleJsonWebSignature.Payload payload =
                await GoogleJsonWebSignature.ValidateAsync(tokenResponse.IdToken);

            // Validate the audience (client ID)
            if (!payload.Audience.Equals(_googleAuthSettings.ClientId))
            {
                throw new BadRequestException("Invalid audience: Client ID mismatch.");
            }

            // Validate the issuer
            if (!payload.Issuer.Equals("accounts.google.com") &&
                !payload.Issuer.Equals("https://accounts.google.com"))
            {
                throw new BadRequestException("Invalid issuer: Not a trusted Google account issuer.");
            }

            // Validate the expiration time
            if (payload.ExpirationTimeSeconds == null)
            {
                throw new BadRequestException("Invalid token: Missing expiration time.");
            }
            else
            {
                DateTime now = DateTime.Now.ToUniversalTime();
                DateTime expiration = DateTimeOffset.FromUnixTimeSeconds((long)payload.ExpirationTimeSeconds)
                    .DateTime;
                if (now > expiration)
                {
                    throw new BadRequestException("Invalid token: Token has expired.");
                }
            }

            // Retrieve user information
            var userSpec = new BaseSpecification<User>(x => x.Email == payload.Email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Add new 
            {
                // Initialize authenticate user
                var userId = Guid.NewGuid();
                user = new()
                {
                    UserId = userId,
                    Email = payload.Email,
                    Avatar = payload.Picture,
                    FirstName = payload.GivenName ?? string.Empty,
                    LastName = payload.FamilyName ?? string.Empty,
                    CreateDate = DateTime.UtcNow,
                };

                // Progress create new user
                user = await CreateNewUserAsync(user);
            }
            else
            {
                // Check if account required MFA
                if (user.TwoFactorEnabled)
                {
                    return new ServiceResult(ResultCodeConst.Auth_Warning0010,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010), user.Email);
                }
            }

            // Try to authenticate user
            return await AuthenticateUserAsync(user != null ? _mapper.Map<UserDto>(user) : null);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (InvalidJwtException ex)
        {
            _logger.Error(ex.Message);
            // Invalid JWT exception handling
            throw new UnauthorizedAccessException("Invalid token: invalid JWT.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error(ex.Message);
            throw;
        }
        catch (TokenResponseException trEx)
        {
            _logger.Error(
                "Token exchange failed: error = {Error}, description = {Desc}, uri = {Uri}",
                trEx.Error.Error,
                trEx.Error.ErrorDescription,
                trEx.Error.ErrorUri);
            throw new Exception(trEx.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception(ex.Message);
            // throw new Exception("An error occurred during Google sign-in.", ex);
        }
    }

    public async Task<IServiceResult> RefreshTokenAsync(string accessToken, string refreshTokenId)
    {
        // Try to validate and extract claims from access token
        var token = new JwtUtils(_tokenValidationParams).ValidateExpiredAccessToken(accessToken);

        // Retrieve claims from the authenticated user's identity
        var roleName = token?.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.Role)?.Value;
        var email = token?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        var name = token?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
        var tokenId = token?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(email) // Is not exist email claim
            || string.IsNullOrEmpty(roleName) // Is not exist role claim
            || string.IsNullOrEmpty(name) // Is not exist name claim
            || string.IsNullOrEmpty(tokenId)) // Is not exist tokenId claim
        {
            // 401
            throw new UnauthorizedException("Missing token claims.");
        }

        // Check exist refresh token by tokenId and refreshTokenId
        var getRefreshTokenResult = await _refreshTokenService.GetByTokenIdAndRefreshTokenIdAsync(
            tokenId, refreshTokenId);
        if (getRefreshTokenResult.Data != null) // Exist refresh token
        {
            // Map to RefreshTokenDto
            var refreshTokenDto = (getRefreshTokenResult.Data as RefreshTokenDto)!;
            // Retrieve refresh token limit
            var maxRefreshTokenLifeSpan = _webTokenSettings.MaxRefreshTokenLifeSpan;
            // Check whether valid refresh token limit
            if (refreshTokenDto.RefreshCount + 1 > maxRefreshTokenLifeSpan)
            {
                throw new ForbiddenException(
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0002));
            }

            // Generate new tokenId
            tokenId = Guid.NewGuid().ToString();
            // Update refresh token 
            refreshTokenDto.TokenId = tokenId;
            // refreshTokenDto.RefreshTokenId = new JwtUtils().GenerateRefreshToken();
            refreshTokenDto.RefreshCount += 1;

            // Progress update
            var updateResult = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
            if (updateResult.ResultCode == ResultCodeConst.SYS_Success0003) // Update success
            {
                // Retrieve user information
                var userSpec = new BaseSpecification<User>(u => u.Email == email);
                // Apply include
                userSpec.ApplyInclude(q => q.Include(u => u.Role));
                var user = await _userRepository.GetWithSpecAsync(userSpec);
                if (user == null)
                {
                    // Not found {0}
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, "email"));
                }

                // Generate access token
                var generateResult = await new JwtUtils(_webTokenSettings)
                    .GenerateJwtTokenAsync(tokenId: tokenId, user: _mapper.Map<UserDto>(user));

                return new ServiceResult(ResultCodeConst.Auth_Success0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0008),
                    new AuthenticateResultDto
                    {
                        AccessToken = generateResult.AccessToken,
                        RefreshToken = refreshTokenDto.RefreshTokenId,
                        ValidTo = generateResult.ValidTo
                    });
            }
        }

        // Resp not found 
        return new ServiceResult(ResultCodeConst.Auth_Warning0002,
            await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0002));
    }

    public async Task<IServiceResult> ChangePasswordAsync(
        string email, string password, string? token = null)
    {
        try
        {
            // Validate token (if any)
            if (!string.IsNullOrEmpty(token))
            {
                var jwtToken = await (new JwtUtils(_tokenValidationParams).ValidateAccessTokenAsync(token));

                if (jwtToken == null)
                {
                    return new ServiceResult(
                        resultCode: ResultCodeConst.Auth_Fail0001,
                        message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0001));
                }
            }

            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Hash password
            var newPasswordHash = HashUtils.HashPassword(password);
            // Update password field
            user.PasswordHash = newPasswordHash;
            // Mark as modified
            await _userRepository.UpdateAsync(user);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                return new ServiceResult(
                    resultCode: ResultCodeConst.Auth_Success0006,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0006),
                    data: true);
            }

            return new ServiceResult(ResultCodeConst.Auth_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0001), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception(ex.Message);
        }
    }

    public async Task<IServiceResult> VerifyChangePasswordOtpAsync(string email, string otp)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // As user account
            if (user.EmailVerificationCode == otp)
            {
                // Generate password reset token (for user)
                var recoveryPasswordToken = await new JwtUtils(_webTokenSettings)
                    .GeneratePasswordResetTokenAsync(_mapper.Map<UserDto>(user));

                // Remove confirmation code
                user.EmailVerificationCode = null!;
                // Mark as modified
                await _userRepository.UpdateAsync(user);
                // Save DB
                var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
                if (isSaved)
                {
                    return new ServiceResult(ResultCodeConst.Auth_Success0009,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0009),
                        new RecoveryPasswordResultDto()
                        {
                            Token = recoveryPasswordToken,
                            Email = user.Email
                        });
                }

                // Mark as update fail
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }

            // Not match OTP
            return new ServiceResult(ResultCodeConst.Auth_Warning0005,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress verify otp");
        }
    }

    public async Task<IServiceResult> ForgotPasswordAsync(string email)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Generate confirmation code
            var otpCode = StringUtils.GenerateUniqueCode();
            // Email subject
            var emailSubject = "[ProposalGuard] - Khôi phục mật khẩu";
            // Email content
            var emailContent = $@"
			    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
			        <h3>Chào {user.FirstName} {user.LastName},</h3>
			        <p>Đây là mã xác nhận để khôi phục mật khẩu của bạn:</p>
			        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
			        <p>Vui lòng sử dụng mã này để hoàn thành quy trình khôi phục mật khẩu.</p>
			        <br />
			        <p style='font-size: 16px;'>Cảm ơn,</p>
			        <p style='font-size: 16px;'>ProposalGuard</p>
			    </div>";

            var isOtpSent = await SendAndSaveOtpAsync(otpCode, user, emailSubject, emailContent);
            if (isOtpSent) // Email sent
            {
                return new ServiceResult(ResultCodeConst.Auth_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005),
                    new RecoveryPasswordResultDto()
                    {
                        Email = user.Email
                    });
            }

            // Fail to send email
            return new ServiceResult(ResultCodeConst.Auth_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception(ex.Message);
        }
    }

    public async Task<IServiceResult> ResendOtpAsync(string email)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Generate confirmation code
            var otpCode = StringUtils.GenerateUniqueCode();
            // Email subject
            var emailSubject = "[ProposalGuard] - Gửi lại email xác nhận";

            // Email content
            var emailContent = $@"
			    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
			        <h3>Chào {user.FirstName} {user.LastName},</h3>
			        <p>Đây là mã xác nhận của bạn:</p>
			        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
			        <p>Vui lòng sử dụng mã này để hoàn tất quá trình.</p>
			        <br />
			        <p style='font-size: 16px;'>Cảm ơn,</p>
			        <p style='font-size: 16px;'>ProposalGuard</p>
			    </div>";

            var isOtpSent = await SendAndSaveOtpAsync(otpCode, user, emailSubject, emailContent);
            if (isOtpSent) // Email sent
            {
                return new ServiceResult(ResultCodeConst.Auth_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005));
            }
            else // Fail to send email
            {
                return new ServiceResult(ResultCodeConst.Auth_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress resend otp");
        }
    }

    public async Task<IServiceResult> EnableMfaAsync(string email)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Handle enable MFA
            // Check whether user or employee already enable MFA
            if (user.TwoFactorEnabled) // Already enabled
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0009));
            }

            // Generate secret key
            var secretKey = TwoFactorAuthUtils.GenerateSecretKey();
            // Generate backup codes
            var backupCodes = TwoFactorAuthUtils.GenerateBackupCodes();
            // Generate QrCode URI
            var uri = TwoFactorAuthUtils.GenerateQrCodeUri(email, secretKey, "ProposalGuard");
            // Generate QrCode bytes from URI
            var qrCode = TwoFactorAuthUtils.GenerateQrCode(uri);
            // Hash backup codes
            var hashedBackupCodes = TwoFactorAuthUtils.EncryptBackupCodes(backupCodes, _appSettings);

            // Progress update MFA key and backup codes
            user.TwoFactorSecretKey = secretKey;
            user.TwoFactorBackupCodes = string.Join(",", hashedBackupCodes);

            // Mark as modified
            await _userRepository.UpdateAsync(user);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            // Check whether enable success
            if (isSaved)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001),
                    new EnableMfaResultDto()
                    {
                        QrCodeImage = $"data:image/png;base64, {Convert.ToBase64String(qrCode)}",
                        BackupCodes = backupCodes,
                    });
            }

            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress enable mfa");
        }
    }

    public async Task<IServiceResult> GetMfaBackupAsync(string email)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Handle get MFA code
            // Check whether user or employee already enable MFA
            if (string.IsNullOrEmpty(user.TwoFactorSecretKey)
                || !user.TwoFactorEnabled) // Not enabled yet
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0011,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
            }

            if (string.IsNullOrEmpty(user.TwoFactorBackupCodes))
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            // Split user backup code by comma
            var hashedCodes = user.TwoFactorBackupCodes.Split(',');

            // Verify Backup Code
            var decryptedBackupCodes = TwoFactorAuthUtils.DecryptBackupCodes(hashedCodes, _appSettings);
            if (decryptedBackupCodes.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), decryptedBackupCodes);
            }

            // Fail to get data 
            return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process get MFA backup codes");
        }
    }

    public async Task<IServiceResult> ValidateMfaAsync(string email, string otp)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Check whether user or employee already enable MFA
            if (string.IsNullOrEmpty(user.TwoFactorSecretKey)) // Not enabled yet
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0011,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
            }

            // Verify OTP base on user MFA secret 
            var isValid = TwoFactorAuthUtils.VerifyOtp(user.TwoFactorSecretKey ?? string.Empty, otp);

            if (isValid)
            {
                // Change account 2FA status
                user.TwoFactorEnabled = true;

                // Authenticate user
                return await AuthenticateUserAsync(_mapper.Map<UserDto>(user));
            }

            // Invalid OTP 
            return new ServiceResult(ResultCodeConst.Auth_Warning0005,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress validate mfa");
        }
    }

    public async Task<IServiceResult> ValidateMfaBackupCodeAsync(string email, string backupCode)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Handle validate MFA
            // Check whether user or employee already enable MFA
            if (string.IsNullOrEmpty(user.TwoFactorSecretKey)
                || !user.TwoFactorEnabled) // Not enabled yet
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0011,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
            }

            // Mark as all backup codes of user have been used
            if (string.IsNullOrEmpty(user.TwoFactorBackupCodes))
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0012,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0012));
            }

            // Split user backup code by comma
            var hashedCodes = user.TwoFactorBackupCodes.Split(',');

            // Verify Backup Code
            var matchingHash = TwoFactorAuthUtils.VerifyBackupCodeAndGetMatch(backupCode, hashedCodes, _appSettings);
            if (matchingHash != null)
            {
                // Remove the matching hash 
                hashedCodes = hashedCodes.Where(hash => hash != matchingHash).ToArray();

                // Progress update backup codes
                user.TwoFactorBackupCodes = string.Join(",", hashedCodes);

                // Mark as modified
                await _userRepository.UpdateAsync(user);
                var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
                if (isSaved)
                {
                    // Authenticate user
                    return await AuthenticateUserAsync(_mapper.Map<UserDto>(user));
                }
            }

            // Invalid backup code
            return new ServiceResult(ResultCodeConst.Auth_Warning0012,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0012));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process validate backup code");
        }
    }

    public async Task<IServiceResult> RegenerateMfaBackupCodeAsync(string email)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            // Handle regenerate backup code
            // Check whether user or employee already enable MFA
            if (string.IsNullOrEmpty(user.TwoFactorSecretKey)
                || !user.TwoFactorEnabled) // Not enabled yet
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0011,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
            }

            // Sending email & generate confirm token
            // Generate confirmation code
            var otpCode = StringUtils.GenerateUniqueCode();
            // Email subject
            var emailSubject = "[ProposalGuard] - Xác nhận yêu cầu tạo lại mã";
            // Email content
            var emailContent = $@"
				    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
				        <h3>Chào {user.FirstName} {user.LastName},</h3>
				        <p>Đây là mã xác nhận của bạn:</p>
				        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
				        <p>Vui lòng sử dụng mã này để hoàn thành quá trình tạo lại mã dự phòng.</p>
				        <br />
				        <p style='font-size: 16px;'>Cảm ơn,</p>
				        <p style='font-size: 16px;'>ProposalGuard</p>
				    </div>";


            // Generate MFA backup token
            var token = await new JwtUtils(_webTokenSettings).GenerateMfaTokenAsync(
                _mapper.Map<UserDto>(user));

            var isOtpSent = await SendAndSaveOtpAsync(otpCode, user, emailSubject, emailContent);
            if (isOtpSent) // Email sent
            {
                return new ServiceResult(ResultCodeConst.Auth_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005),
                    new RegenerateMfaBackupResultDto()
                    {
                        Token = token
                    });
            }
            else
            {
                return new ServiceResult(ResultCodeConst.Auth_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while regenerating backup code");
        }
    }

    public async Task<IServiceResult> ConfirmRegenerateMfaBackupCodeAsync(string email, string otp, string token)
    {
        try
        {
            // Retrieve user by email
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.Role));
            var user = await _userRepository.GetWithSpecAsync(userSpec);
            if (user == null) // Not exist
            {
                var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errorMSg, "email"));
            }

            if (user.EmailVerificationCode == otp) // Is valid OTP
            {
                // Check whether user or employee already enable MFA
                if (string.IsNullOrEmpty(user.TwoFactorSecretKey)
                    || !user.TwoFactorEnabled) // Not enabled yet
                {
                    return new ServiceResult(ResultCodeConst.Auth_Warning0011,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
                }

                // Validate Mfa token 
                var jwtToken = await new JwtUtils(_tokenValidationParams).ValidateMfaTokenAsync(token);
                if (jwtToken == null || user.TwoFactorSecretKey == null)
                {
                    return new ServiceResult(ResultCodeConst.Auth_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0003));
                }

                // Generate new backup codes
                // Generate backup codes
                var backupCodes = TwoFactorAuthUtils.GenerateBackupCodes();
                // Hash backup codes
                var hashedBackupCodes = TwoFactorAuthUtils.EncryptBackupCodes(backupCodes, _appSettings);

                // Stored secret key and backup codes
                user.TwoFactorBackupCodes = string.Join(",", hashedBackupCodes);
                // Mark as modified
                await _userRepository.UpdateAsync(user);
                // Save DB
                var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
                if (isSaved)
                {
                    // Create MFA backup codes message
                    return new ServiceResult(ResultCodeConst.Auth_Success0010,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0010));
                }

                // Fail to regenerate MFA backup codes
                return new ServiceResult(ResultCodeConst.Auth_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0003));
            }

            // Not match OTP
            return new ServiceResult(ResultCodeConst.Auth_Warning0005,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process confirm regenerate MFA backup code");
        }
    }

    public async Task<IServiceResult> SoftDeleteAsync(Guid userId)
    {
        try
        {
            // Check exist user
            var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            // Check if user account already mark as deleted
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "user"));
            }

            // Update delete status
            existingEntity.IsDeleted = true;

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0007,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process soft delete user");
        }
    }

    public async Task<IServiceResult> SoftDeleteRangeAsync(Guid[] userIds)
    {
        try
        {
            // Get all matching user 
            // Build spec
            var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
            var userEntities = await _unitOfWork.Repository<User, Guid>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var userList = userEntities.ToList();
            if (userList.Any(x => x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Progress update deleted status to true
            userList.ForEach(x => x.IsDeleted = true);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0007,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when remove range user");
        }
    }

    public async Task<IServiceResult> UndoDeleteAsync(Guid userId)
    {
        try
        {
            // Check exist user
            var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            // Check if user account already mark as deleted
            if (existingEntity == null || !existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "user"));
            }

            // Update delete status
            existingEntity.IsDeleted = false;

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete user");
        }
    }

    public async Task<IServiceResult> UndoDeleteRangeAsync(Guid[] userIds)
    {
        try
        {
            // Get all matching user 
            // Build spec
            var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
            var userEntities = await _unitOfWork.Repository<User, Guid>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var userList = userEntities.ToList();
            if (userList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Progress undo deleted status to false
            userList.ForEach(x => x.IsDeleted = false);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete range");
        }
    }

    public override async Task<IServiceResult> DeleteAsync(Guid id)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "user"));
            }

            // Check whether user in the trash bin
            if (!existingEntity.IsDeleted)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Process add delete entity
            await _unitOfWork.Repository<User, Guid>().DeleteAsync(id);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
            }
            else
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0004;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);
                serviceResult.Data = false;
            }
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 547: // Foreign key constraint violation
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
                }
            }

            // Throw if other issues
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete user");
        }

        return serviceResult;
    }

    public async Task<IServiceResult> DeleteRangeAsync(Guid[] userIds)
    {
        try
        {
            // Get all matching user 
            // Build spec
            var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
            var userEntities = await _unitOfWork.Repository<User, Guid>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var userList = userEntities.ToList();
            if (userList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Process delete range
            await _unitOfWork.Repository<User, Guid>().DeleteRangeAsync(userIds);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, userList.Count.ToString()), true);
            }

            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 547: // Foreign key constraint violation
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
                }
            }

            // Throw if other issues
            throw new Exception("Error invoke when process delete range user");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range user");
        }
    }

    public async Task<IServiceResult> CreateUnexistingUsersAsync(List<UserDto> dtos)
    {
        try
        {
            var roles = dtos.Select(x => x.RoleId).Distinct().ToList();
            if (roles.Count > 1)
            {
                return new ServiceResult(ResultCodeConst.Review_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Review_Warning0002));
            }

            // Get role
            // Check role existence
            var roleEntity = await _roleRepository.GetByIdAsync(roles.First());
            if (roleEntity == null)
            {
                // Not found
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002,
                    message: StringUtils.Format(errMsg, "vai trò"));
            }

            // Add new entity
            var entities = (_mapper.Map<List<User>>(dtos)).AsEnumerable();
            await _userRepository.AddRangeAsync(entities);
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001),
                _mapper.Map<List<UserDto>>(entities));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range user");
        }
    }

    public async Task<IServiceResult> GetReviewerByProposal(int proposalId)
    {
        try
        {
            // get latest historyId of proposal
            var latestHistory = await _historyService.GetLatestHistoryByProposalIdAsync(proposalId);
            if (latestHistory.Data == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002));
            }

            var history = (latestHistory.Data as ProposalHistoryDto)!;
            var userBaseSpec = new BaseSpecification<User>(x => x.ReviewSessions
                .Select(rs => rs.HistoryId).Contains(history.HistoryId));
            userBaseSpec.ApplyInclude(q => q.Include(u => u.ReviewSessions));
            var users = await _userRepository.GetAllWithSpecAsync(userBaseSpec);
            if (!users.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002));
            }
            var localConfig = new TypeAdapterConfig();
            localConfig.NewConfig<User, UserDto>()
                .Ignore(dest => dest.PasswordHash!)
                .Ignore(dest => dest.RoleId)
                .Ignore(dest => dest.EmailConfirmed)
                .Ignore(dest => dest.TwoFactorEnabled)
                .Ignore(dest => dest.PhoneNumberConfirmed)
                .Ignore(dest => dest.TwoFactorSecretKey!)
                .Ignore(dest => dest.TwoFactorBackupCodes!)
                .Ignore(dest => dest.PhoneVerificationCode!)
                .Ignore(dest => dest.EmailVerificationCode!)
                .Ignore(dest => dest.PhoneVerificationExpiry!)
                .Ignore(dest => dest.ReviewSessions)
                .Map(dto => dto.Role, src => src.Role)
                .AfterMapping((src, dest) => { dest.Role.RoleId = 0; }); 
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                users.Adapt<List<UserDto>>(localConfig));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get reviewer");
        }
    }

    // Send email (confirmation/OTP)
    private async Task<bool> SendAndSaveOtpAsync(string otpCode, User user,
        string subject, string emailContent)
    {
        try
        {
            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { user.Email },
                // Define subject
                subject: subject,
                // Add email body content
                content: emailContent
            );

            // Send email
            await _emailService.SendEmailAsync(message: emailMessageDto, isBodyHtml: true);

            // Progress update email confirmation code to DB
            user.EmailVerificationCode = otpCode;
            // Mark as modified
            await _userRepository.UpdateAsync(user);
            // Save DB            
            return await _unitOfWork.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            // Log the exception or handle errors
            _logger.Error("Failed to send confirmation email: {msg}", ex.Message);
        }

        return false;
    }

    // Validate password
    private bool ValidatePassword(string? inputPassword, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            throw new UnauthorizedException(
                "Your password is not set. Please sign in with an external provider to update it.");
        }

        if (!HashUtils.VerifyPassword(inputPassword ?? string.Empty, storedHash))
        {
            return false;
        }

        return true;
    }

    // Authenticate user
    private async Task<ServiceResult> AuthenticateUserAsync(UserDto? user)
    {
        // Check not exist authenticate user (save fail,...) 
        if (user == null)
            throw new Exception("Unknown error invoke while authenticating user.");

        // Validate user status
        if (user.UserId == Guid.Empty || string.IsNullOrEmpty(user.Role.RoleName))
        {
            return new ServiceResult(ResultCodeConst.Auth_Warning0007,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0007));
        }

        if (!user.IsActive)
        {
            return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
        }

        // Generate token
        var tokenId = Guid.NewGuid().ToString();
        var jwtResponse = await new JwtUtils(_webTokenSettings).GenerateJwtTokenAsync(
            tokenId: tokenId, user: _mapper.Map<UserDto>(user));

        if (string.IsNullOrEmpty(jwtResponse.AccessToken) || jwtResponse.ValidTo <= DateTime.UtcNow)
            throw new Exception("Invalid JWT token generated");

        // Handle refresh token
        var refreshTokenDto = await HandleRefreshTokenAsync(user, tokenId);

        return new ServiceResult(
            ResultCodeConst.Auth_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0002),
            new AuthenticateResultDto
            {
                AccessToken = jwtResponse.AccessToken,
                RefreshToken = refreshTokenDto.RefreshTokenId,
                ValidTo = jwtResponse.ValidTo
            });
    }

    // Handle refresh token
    private async Task<RefreshTokenDto> HandleRefreshTokenAsync(UserDto user, string tokenId)
    {
        var getTokenResult = await _refreshTokenService.GetByUserIdAsync(user.UserId);
        if (getTokenResult.Data is null)
        {
            return await CreateNewRefreshTokenAsync(user, tokenId);
        }

        return await UpdateExistingRefreshTokenAsync((RefreshTokenDto)getTokenResult.Data, tokenId);
    }

    // Create new refresh token 
    private async Task<RefreshTokenDto> CreateNewRefreshTokenAsync(UserDto user, string tokenId)
    {
        var refreshTokenId = await new JwtUtils().GenerateRefreshTokenAsync();

        var refreshTokenDto = new RefreshTokenDto
        {
            CreateDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMinutes(_webTokenSettings.RefreshTokenLifeTimeInMinutes),
            RefreshTokenId = refreshTokenId,
            RefreshCount = 0,
            TokenId = tokenId,
            UserId = user.UserId
        };

        var result = await _refreshTokenService.CreateAsync(refreshTokenDto);
        if (result.ResultCode != ResultCodeConst.SYS_Success0001)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
            throw new Exception(StringUtils.Format(errMsg, "refresh token"));
        }

        return refreshTokenDto;
    }

    // Update existing refresh token
    private async Task<RefreshTokenDto> UpdateExistingRefreshTokenAsync(RefreshTokenDto refreshTokenDto, string tokenId)
    {
        refreshTokenDto.CreateDate = DateTime.UtcNow;
        refreshTokenDto.RefreshTokenId = await new JwtUtils().GenerateRefreshTokenAsync();
        refreshTokenDto.TokenId = tokenId;
        refreshTokenDto.RefreshCount = 0;

        var result = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
        if (result.ResultCode != ResultCodeConst.SYS_Success0003)
        {
            throw new Exception(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }

        return refreshTokenDto;
    }

    // Create user 
    private async Task<User?> CreateNewUserAsync(User user)
    {
        // General member role
        var roleSpec = new BaseSpecification<SystemRole>(r => r.RoleName == nameof(Role.Lecturer));
        var role = (await _roleService.GetWithSpecAsync(
            roleSpec)).Data as SystemRoleDto;
        if (role == null)
        {
            throw new NotFoundException("Default role is not found to create new user");
        }

        ;

        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // Update shared fields
        user.CreateDate = currentLocalDateTime;
        user.RoleId = role.RoleId;
        // Update security fields based on provider
        user.EmailConfirmed = true;
        user.IsActive = true;
        user.IsDeleted = false;
        user.PhoneNumberConfirmed = false;
        user.TwoFactorEnabled = false;

        // Progress create new user
        await _userRepository.AddAsync(user);

        // Assign ID and set to active if success to mark as authenticate success
        if (await _unitOfWork.SaveChangesAsync() > 0)
        {
            return user;
        }

        return null;
    }

    // Handle get google token response
    private async Task<TokenResponse> ExchangeCodeForAccessTokenAsync(string code)
    {
        // Use GoogleAuthorizationCodeFlow to handle the token exchange
        var tokenRequest = new AuthorizationCodeTokenRequest()
        {
            Code = code,
            ClientId = _googleAuthSettings.ClientId,
            ClientSecret = _googleAuthSettings.ClientSecret,
            RedirectUri = _googleAuthSettings.RedirectUri,
            GrantType = OpenIdConnectGrantTypes.AuthorizationCode,
        };

        // Execute the token exchange
        var tokenResponse = await tokenRequest.ExecuteAsync(
            clock: SystemClock.Default,
            httpClient: _httpClient,
            taskCancellationToken: CancellationToken.None,
            tokenServerUrl: GoogleAuthConsts.OidcTokenUrl);

        // Return the token response
        return tokenResponse;
    }
}