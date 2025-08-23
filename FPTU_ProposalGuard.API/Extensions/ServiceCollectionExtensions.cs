using System.Text;
using CloudinaryDotNet;
using FluentValidation;
using FPTU_ProposalGuard.Application.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace FPTU_ProposalGuard.API.Extensions
{
    //  Summary:
    //      This class is to configure services for presentation layer 
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureEndpoints(this IServiceCollection services)
        {
            // Add controllers
            services.AddControllers();
            // Configures ApiExplorer
            services.AddEndpointsApiExplorer();
            // Add swagger
            services.AddSwaggerGen();
            // Add HttpContextAccessor
            services.AddHttpContextAccessor();
            // Add HttpClient
            services.AddHttpClient();

            return services;
        }

        public static IServiceCollection ConfigureSerilog(this IServiceCollection services,
            WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext:l} - {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext:l} - {Message:lj}{NewLine}{Exception}")
                .Enrich.WithProperty("Environment", builder.Environment)
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Host.UseSerilog();

            // Register the Serilog logger
            services.AddSingleton(Log.Logger);

            return services;
        }

        public static IServiceCollection ConfigureAppSettings(this IServiceCollection services,
            WebApplicationBuilder builder,
            IWebHostEnvironment env)
        {
            // Configure AppSettings
            services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
            // Configure WebTokenSettings
            services.Configure<WebTokenSettings>(builder.Configuration.GetSection("WebTokenSettings"));
            // Configure GoogleAuthSettings
            services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuthSettings"));
            // Configure CheckProposalSettings
            services.Configure<CheckProposalSettings>(builder.Configuration.GetSection("CheckProposalSettings"));
            // Configure AmazonS3Settings
            services.Configure<AmazonS3Settings>(builder.Configuration.GetSection("AmazonS3Settings"));
            return services;
        }

        public static IServiceCollection ConfigureCloudinary(this IServiceCollection services,
            IConfiguration configuration)
        {
            Cloudinary cloudinary = new Cloudinary(configuration["CloudinarySettings:CloudinaryUrl"]!)
            {
                Api = { Secure = true }
            };

            services.AddSingleton(cloudinary);

            return services;
        }

        public static IServiceCollection ConfigureSignalR(this IServiceCollection services)
        {
            services.AddSignalR();
            return services;
        }

        public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection ConfigureCamelCaseForValidation(this IServiceCollection services)
        {
            ValidatorOptions.Global.PropertyNameResolver = CamelCasePropertyNameResolver.ResolvePropertyName;

            return services;
        }

        public static IServiceCollection AddAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Define TokenValidationParameters
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = bool.Parse(configuration["WebTokenSettings:ValidateIssuerSigningKey"]!),
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["WebTokenSettings:IssuerSigningKey"]!)),
                ValidateIssuer = bool.Parse(configuration["WebTokenSettings:ValidateIssuer"]!),
                ValidAudience = configuration["WebTokenSettings:ValidAudience"],
                ValidIssuer = configuration["WebTokenSettings:ValidIssuer"],
                ValidateAudience = bool.Parse(configuration["WebTokenSettings:ValidateAudience"]!),
                RequireExpirationTime = bool.Parse(configuration["WebTokenSettings:RequireExpirationTime"]!),
                ValidateLifetime = bool.Parse(configuration["WebTokenSettings:ValidateLifetime"]!),
                ClockSkew = TimeSpan.Zero
            };

            // Register TokenValidationParameters in the DI container
            services.AddSingleton(tokenValidationParameters);

            // Add authentication
            services.AddAuthentication(options =>
            {
                // Define default scheme
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // For API requests
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // For login challenge
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => // Enables JWT-bearer authentication
            {
                // Disable Https required for the metadata address or authority
                options.RequireHttpsMetadata = false;
                // Define type and definitions required for validating a token
                options.TokenValidationParameters = tokenValidationParameters;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        public static IServiceCollection AddCors(this IServiceCollection services, string policyName)
        {
            // Configure CORS
            services.AddCors(p => p.AddPolicy(policyName, policy =>
            {
                // allow all with any header, method
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }));

            return services;
        }

        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            return services.AddTransient(
                typeof(Lazy<>),
                typeof(LazilyResolved<>));
        }

        private class LazilyResolved<T> : Lazy<T>
        {
            public LazilyResolved(IServiceProvider serviceProvider)
                : base(serviceProvider.GetRequiredService<T>)
            {
            }
        }
    }
}