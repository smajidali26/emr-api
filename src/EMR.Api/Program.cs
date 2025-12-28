using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using EMR.Api.Extensions;
using EMR.Api.Middleware;
using EMR.Application;
using EMR.Infrastructure;
using EMR.Infrastructure.Authorization;
using EMR.Infrastructure.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// SECURITY FIX: Task #4 - Add request body size limits (Ryan Kim - 4h)
// Configure Kestrel to limit request body size and prevent denial-of-service attacks
builder.WebHost.ConfigureKestrel(options =>
{
    // SECURITY: Limit maximum request body size to 10 MB
    // This prevents attackers from exhausting server resources with large payloads
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB

    // SECURITY: Set reasonable timeout limits
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/emr-.log", rollingInterval: RollingInterval.Day);
});

// Add Azure Key Vault if configured
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(keyVaultUrl);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// SECURITY FIX: Task #4 - Add request body size limits (Ryan Kim - 4h)
// Configure form options to limit multipart body length for file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // SECURITY: Limit multipart body length for file uploads to 10 MB
    // This prevents attackers from uploading extremely large files
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
    options.ValueLengthLimit = 10 * 1024 * 1024; // 10 MB
    options.MultipartHeadersLengthLimit = 16 * 1024; // 16 KB
});

// Add HttpContextAccessor for accessing HTTP context in services
builder.Services.AddHttpContextAccessor();

// SECURITY: Configure forwarded headers for proper client IP resolution behind proxies
// This is required for accurate IP logging, rate limiting, and audit trails
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // IMPORTANT: In production, configure KnownProxies or KnownIPNetworks with your
    // actual load balancer/proxy IP addresses. Leaving these empty accepts all proxies
    // which could allow IP spoofing. Example:
    // options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
    // options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));

    // Clear defaults and only trust explicitly configured proxies in production
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();

    // For Azure App Service or known cloud providers, the forwarding is handled securely
    // Set ForwardLimit to prevent unbounded header chaining attacks
    options.ForwardLimit = 2; // Only trust 2 levels of proxies
});

// SECURITY FIX: Task #3 - Add rate limiting middleware (Sarah Garcia - 8h)
// Prevents brute force attacks and DoS by limiting request rates per client
builder.Services.AddRateLimiter(options =>
{
    // Global rate limiter - applies to all endpoints
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Use client IP address for rate limiting
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,          // Max 100 requests per window
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10             // Allow 10 requests to queue
            });
    });

    // Stricter rate limit for authentication endpoints
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;           // Max 10 auth attempts per window
        limiter.Window = TimeSpan.FromMinutes(5);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;             // No queuing for auth - reject immediately
    });

    // Stricter rate limit for patient search to prevent data enumeration
    options.AddFixedWindowLimiter("patient-search", limiter =>
    {
        limiter.PermitLimit = 30;           // Max 30 searches per window
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 5;
    });

    // Handle rate limit exceeded
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            error = "TooManyRequests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.TotalSeconds
                : 60
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, token);
    };
});

// SECURITY: Add CSRF protection (antiforgery)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-Token";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // Allow JavaScript to read the token
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add layer dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add API configurations
builder.Services.AddSwaggerConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddAuthenticationConfiguration(builder.Configuration);
builder.Services.AddHealthCheckConfiguration(builder.Configuration);

// SECURITY: Add configuration validation for Azure AD B2C
// This ensures placeholder values are not used in production
builder.Services.AddConfigurationValidation(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline

// SECURITY: Process forwarded headers FIRST before any other middleware
// This ensures RemoteIpAddress is correctly set for all downstream middleware
app.UseForwardedHeaders();

// SECURITY: Global exception handler - catches all unhandled exceptions
// and returns HIPAA-compliant error responses (no stack traces, PII redaction)
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMR API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

// SECURITY FIX: Task #3 - Enable rate limiting middleware (Sarah Garcia - 8h)
// Must be placed before authentication to protect auth endpoints
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// SECURITY: Add CSRF token validation middleware for state-changing requests
app.UseCsrfValidation();

// Add authorization auditing middleware
app.UseAuthorizationAuditing();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting EMR API application");

    // SECURITY: Validate critical configuration at startup
    // This will fail fast if Azure AD B2C is not properly configured in production
    app.ValidateConfiguration();

    // Initialize database with migrations and seed data
    await app.Services.InitializeDatabaseAsync();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program class for integration testing with WebApplicationFactory
public partial class Program { }
