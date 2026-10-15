using FluentValidation;
using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Controllers;
using ITender.Integration.Api.Infrastructure.Crm;
using ITender.Integration.Api.Infrastructure.ETender;
using ITender.Integration.Api.Infrastructure.Security;
using ITender.Integration.Api.Middleware;
using ITender.Integration.Api.Services;
using ITender.Integration.Api.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
builder.Services.Configure<CrmOptions>(builder.Configuration.GetSection(CrmOptions.Section));
builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection(EncryptionOptions.Section));
builder.Services.Configure<ETenderApiOptions>(builder.Configuration.GetSection(ETenderApiOptions.Section));
builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.Section));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.Section));

builder.Services.AddSingleton<ISecretProtector, AesSecretProtector>();

// ---- Dataverse (client-credentials) ----
builder.Services.AddSingleton<ServiceClient>(sp =>
{
    var crm = sp.GetRequiredService<IOptions<CrmOptions>>().Value;
    var secret = sp.GetRequiredService<ISecretProtector>().Unprotect(crm.ClientSecret);

    var client = new ServiceClient(
        $"AuthType=ClientSecret;Url={crm.CrmURL};ClientId={crm.ClientId};ClientSecret={secret};RequireNewInstance=true");

    if (!client.IsReady)
        throw new InvalidOperationException($"Dataverse connection failed: {client.LastError}");
    return client;
});
builder.Services.AddSingleton<IOrganizationServiceAsync2>(sp => sp.GetRequiredService<ServiceClient>());

// ---- e-Tender API client ----
builder.Services.AddTransient<ETenderAuthHandler>();
builder.Services.AddHttpClient<IETenderClient, ETenderClient>((sp, http) =>
{
    var baseUrl = sp.GetRequiredService<IOptions<ETenderApiOptions>>().Value.BaseUrl;
    http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
})
    .AddHttpMessageHandler<ETenderAuthHandler>()
    .AddStandardResilienceHandler(o =>
    {
        // Publishing is not known to be idempotent on the e-Tender side, so no automatic POST retries here.
        o.Retry.DisableForUnsafeHttpMethods();
    });

// ---- Application services ----
builder.Services.AddSingleton<ISastClock, SastClock>();
builder.Services.AddSingleton<IWorkingDayCalculator, WorkingDayCalculator>();
builder.Services.AddScoped<IReferenceDataService, PlaceholderReferenceDataService>(); // TODO: Dataverse implementation
builder.Services.AddScoped<IIntegrationTransactionRepository, IntegrationTransactionRepository>();
builder.Services.AddScoped<IIntegrationTransactionService, IntegrationTransactionService>();

// ---- Authentication: CRM credentials -> JWT (FR-001, BR-01) ----
builder.Services.AddSingleton<IPasswordVerifier, PasswordVerifier>();
builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<ICredentialAuthService, CredentialAuthService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ILookupService, LookupService>();

var auth = builder.Configuration.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new AuthOptions();
var signingKey = AuthOptions.BuildKey(auth.SigningKey); // fails fast if missing or too short

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth.Issuer,
            ValidateAudience = true,
            ValidAudience = auth.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        // BRS 18.1: authentication / authorisation failures are logged as security events (never the token).
        o.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Security")
                    .LogWarning("SecurityEvent AuthenticationFailed. Correlation {CorrelationId}, reason {Reason}",
                        ctx.HttpContext.GetCorrelationId(), ctx.Exception.GetType().Name);
                return Task.CompletedTask;
            },
            OnForbidden = ctx =>
            {
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Security")
                    .LogWarning("SecurityEvent AuthorisationFailed. Correlation {CorrelationId}, path {Path}",
                        ctx.HttpContext.GetCorrelationId(), ctx.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAwardTenders", policy =>
        policy.RequireClaim(
            PermissionClaimTypes.CanAwardTenders, "true"));

    options.AddPolicy("CanPracticallyCompleteContracts", policy =>
        policy.RequireClaim(
            PermissionClaimTypes.CanPracticallyCompleteContracts, "true"));

    options.AddPolicy("CanTerminateContracts", policy =>
        policy.RequireClaim(
            PermissionClaimTypes.CanTerminateContracts, "true"));

    options.AddPolicy("CanChangeTenderAndSubmitCR", policy =>
        policy.RequireClaim(
            PermissionClaimTypes.CanChangeTenderAndSubmitCR, "true"));

    options.AddPolicy("CreateAndEditTender", policy =>
        policy.RequireClaim(
            PermissionClaimTypes.CreateAndEditTender, "true"));

    options.AddPolicy("EditAndAdvertiseTender", policy =>
        policy.RequireClaim(
            PermissionClaimTypes.EditAndAdvertiseTender, "true"));
});

// Throttle the token endpoint (brute-force protection, in addition to CRM account lockout)
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// ---- API ----
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "iTender Integration API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionEnvelopeMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
