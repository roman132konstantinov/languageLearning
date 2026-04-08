using System.Threading.RateLimiting;
using API.Infrastructure;
using API.Middleware;
using API.Security;
using Application.Common.Exceptions;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.RequestQuery
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});

builder.Services
    .AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetSection(AuthOptions.SectionName))
    .Validate(options => options.MaxFailedAccessAttempts > 0, "Auth max failed attempts must be greater than 0.")
    .Validate(options => options.LockoutMinutes > 0, "Auth lockout minutes must be greater than 0.")
    .Validate(options => options.PasswordHashIterations >= 100000, "Password hash iterations must be at least 100000.")
    .ValidateOnStart();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey) && options.SigningKey.Length >= 32, "JWT signing key must be at least 32 characters.")
    .Validate(options => options.AccessTokenMinutes > 0, "JWT access token minutes must be greater than 0.")
    .Validate(options => options.RefreshTokenDays > 0, "JWT refresh token days must be greater than 0.")
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));

builder.Services
    .AddOptions<SeedingOptions>()
    .Bind(builder.Configuration.GetSection(SeedingOptions.SectionName));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AuthOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (corsOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var rateLimitingOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = rateLimitingOptions.QueueLimit,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("auth", httpContext =>
    {
        var partitionKey = $"auth:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.AuthPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.AuthWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be configured via environment variables or user secrets.");
}

builder.Services.AddDbContext<LanguageLearningDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>("Bearer", _ => { });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<ILessonWordRepository, LessonWordRepository>();
builder.Services.AddScoped<ILessonWordService, LessonWordService>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IExerciseService, ExerciseService>();
builder.Services.AddScoped<IExerciseOptionRepository, ExerciseOptionRepository>();
builder.Services.AddScoped<IExerciseOptionService, ExerciseOptionService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserWordProgressRepository, UserWordProgressRepository>();
builder.Services.AddScoped<IUserWordProgressService, UserWordProgressService>();
builder.Services.AddScoped<IUserLessonProgressRepository, UserLessonProgressRepository>();
builder.Services.AddScoped<IUserLessonProgressService, UserLessonProgressService>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Request validation failed.",
                StatusCodes.Status401Unauthorized => "Authentication failed.",
                StatusCodes.Status403Forbidden => "Access denied.",
                StatusCodes.Status404NotFound => "Resource not found.",
                StatusCodes.Status409Conflict => "Request conflicts with current data.",
                _ => "An unexpected error occurred."
            },
            Detail = exception?.Message
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await StartupTasks.InitializeDatabaseAsync(app.Services, app.Logger);

app.UseHttpLogging();
app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program;
