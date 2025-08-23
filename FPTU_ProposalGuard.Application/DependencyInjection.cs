using System.Reflection;
using FPTU_ProposalGuard.Application.Dtos.Authentications;
using FPTU_ProposalGuard.Application.Dtos.Notifications;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.Semesters;
using FPTU_ProposalGuard.Application.Dtos.SystemRoles;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Application.Services;
using FPTU_ProposalGuard.Application.Services.IExternalServices;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;

namespace FPTU_ProposalGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register external services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISystemMessageService, SystemMessageService>();
        // Register application services
        services.AddScoped(typeof(IGenericService<,,>), typeof(GenericService<,,>));
        services.AddScoped(typeof(IReadOnlyService<,,>), typeof(ReadOnlyService<,,>));
        services.AddScoped<INotificationService<NotificationDto>, NotificationService>();
        services.AddScoped<INotificationRecipientService<NotificationRecipientDto>, NotificationRecipientService>();
        services.AddScoped<IRefreshTokenService<RefreshTokenDto>, RefreshTokenService>();
        services.AddScoped<IUserService<UserDto>, UserService>();
        services.AddScoped<ISystemRoleService<SystemRoleDto>, SystemRoleService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<IProjectProposalService<ProjectProposalDto>, ProjectProposalService>();
        services.AddScoped<IExtractService,ExtractService>();
        services.AddScoped<IProposalStudentService<ProposalStudentDto>, ProposalStudentService>();
        services.AddScoped<IProposalSupervisorService<ProposalSupervisorDto>, ProposalSupervisorService>();
        services.AddScoped<IProposalHistoryService<ProposalHistoryDto>, ProposalHistoryService>();
        services.AddScoped<IReviewSessionService<ReviewSessionDto>, ReviewSessionService>();
        services.AddScoped<IReviewQuestionService<ReviewQuestionDto>, ReviewQuestionService>();
        services.AddScoped<ISemesterService<SemesterDto>, SemesterService>();
        
        return services
            .ConfigureMapster() // Add mapster
            .ConfigureCloudinary() // Add cloudinary
            .ConfigureExcel(); // Add excel
    }

    private static IServiceCollection ConfigureExcel(this IServiceCollection services)
    {
        // Add License for Excel handler
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        return services;
    }
    
    private static IServiceCollection ConfigureMapster(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Default
            .MapToConstructor(true)
            .PreserveReference(true);
        // Get Mapster GlobalSettings
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        // Scans the assembly and gets the IRegister, adding the registration to the TypeAdapterConfig
        typeAdapterConfig.Scan(Assembly.GetExecutingAssembly());

        // Register the mapper as Singleton service for my application
        var mapperConfig = new Mapper(typeAdapterConfig);
        services.AddSingleton<IMapper>(mapperConfig);

        return services;
    }

    private static IServiceCollection ConfigureCloudinary(this IServiceCollection services)
    {
        // Configure this later...

        return services;
    }
}