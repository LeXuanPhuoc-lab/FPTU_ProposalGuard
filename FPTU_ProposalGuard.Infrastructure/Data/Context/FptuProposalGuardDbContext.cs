using System.Reflection;
using System.Security.Claims;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FPTU_ProposalGuard.Infrastructure.Data.Context;

public class FptuProposalGuardDbContext : DbContext
{
    private readonly IHttpContextAccessor _contextAccessor;
    
    public FptuProposalGuardDbContext()
    {
    }

    public FptuProposalGuardDbContext(DbContextOptions<FptuProposalGuardDbContext> options)
        : base(options)
    {
    }
    
    public FptuProposalGuardDbContext(
        DbContextOptions<FptuProposalGuardDbContext> options,
        IHttpContextAccessor contextAccessor) : base(options)
    {
        _contextAccessor = contextAccessor;
    }

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; }

    public DbSet<ProjectProposal> ProjectProposals { get; set; }

    public DbSet<ProposalHistory> ProposalHistories { get; set; }

    public DbSet<ProposalSimilarity> ProposalSimilarities { get; set; }

    public DbSet<ProposalStudent> ProposalStudents { get; set; }

    public DbSet<ProposalSupervisor> ProposalSupervisors { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Semester> Semesters { get; set; }
    
    public DbSet<ReviewQuestion> ReviewQuestions { get; set; }
    public DbSet<ReviewSession> ReviewSessions { get; set; }

    public DbSet<SystemRole> SystemRoles { get; set; }

    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(GetConnectionString(), o
                => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        }
    }
    
    private string GetConnectionString()
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? null!;
		
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (!string.IsNullOrEmpty(environment))
        {
            builder.AddJsonFile($"appsettings.{environment}.json");
        }

        IConfiguration configuration = builder.Build();

        return configuration.GetConnectionString("DefaultConnectionStr")!;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    // Declare for constant variable
    private const string SystemSource = "system";
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Try to retrieve user email from claims
        var userEmail = _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        // Set auditable properties (createdAt, updatedAt, createdBy, updatedBy)
        SetAuditableProperties(userEmail);

        // Save changes to the database
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Sets auditable properties for entities that are inherited from <see cref="IAuditableEntity"/>
    /// </summary>
    /// <param name="email"></param>
    private void SetAuditableProperties(string? email)
    {
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
		
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = currentLocalDateTime;
                    entry.Entity.CreatedBy = !string.IsNullOrEmpty(email) ? email : SystemSource;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = currentLocalDateTime;
                    entry.Entity.UpdatedBy = !string.IsNullOrEmpty(email) ? email : SystemSource;
                    break;
            }
        }
    }
}

#region Design Time only (Dev only)
// This class only use to invoke commands:
//  + dotnet ef database update
//  + dotnet ef migrations add
public class FptuProposalGuardDbContextFactory : IDesignTimeDbContextFactory<FptuProposalGuardDbContext>
{
    public FptuProposalGuardDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FptuProposalGuardDbContext>();
        
        // Change connection string base on the current env
        optionsBuilder
            .UseSqlServer("Server=(local);Initial Catalog=FPTU_ProposalGuardDB;uid=sa;pwd=1234567890;TrustServerCertificate=True")
            .EnableSensitiveDataLogging();
        
        return new FptuProposalGuardDbContext(optionsBuilder.Options);
    }
}
#endregion