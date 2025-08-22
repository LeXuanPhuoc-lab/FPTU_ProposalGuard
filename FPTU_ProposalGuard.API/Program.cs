using FPTU_ProposalGuard.API.Extensions;
using FPTU_ProposalGuard.API.Middlewares;
using FPTU_ProposalGuard.Application;
using FPTU_ProposalGuard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Add HttpClient
    .AddHttpClient()
    // Add CORS
    .AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy
                .AllowAnyOrigin() // Cho phép mọi domain
                .AllowAnyMethod() // Cho phép mọi HTTP method (GET, POST, PUT, DELETE,...)
                .AllowAnyHeader(); // Cho phép mọi header
        });
    })
    // Add system health checks
    .AddHealthChecks();

builder.Services
    // Configure background services
    .ConfigureBackgroundServices()
    // Configure endpoints, swagger
    .ConfigureEndpoints()
    // Configure Serilog
    .ConfigureSerilog(builder)
    // Configure CamelCase for validation
    .ConfigureCamelCaseForValidation()
    // Configure appSettings
    .ConfigureAppSettings(builder, builder.Environment)
    // Configure Cloudinary
    .ConfigureCloudinary(builder.Configuration);

builder.Services
    // Configure for application layer
    .AddApplication(builder.Configuration)
    // Configure for infrastructure layer
    .AddInfrastructure(builder.Configuration);

builder.Services
    // Add swagger
    .AddSwagger()
    // Add authentication
    .AddAuthentication(builder.Configuration)
    // Add Lazy resolution
    .AddLazyResolution()
    // Add signalR
    .AddSignalR();

var app = builder.Build();

// Register database initializer
app.Lifetime.ApplicationStarted.Register(() => Task.Run(async () => { await app.InitializeDatabaseAsync(); }));

app.WithSwagger();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Custom Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Exception handling middleware

app.MapControllers(); // Maps controller endpoints after middleware pipeline
app.UseCors("AllowAll");

app.Run();