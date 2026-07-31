using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Configuration;
using MemeTokenHub.ClaimService.Api.Health;
using MemeTokenHub.ClaimService.Api.Infrastructure;
using MemeTokenHub.ClaimService.Api.Middleware;
using MemeTokenHub.Shared.Auth;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using MongoDB.Driver;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MemeTokenHub Claim Service API",
        Version = "v1",
        Description = "Private claim evidence, moderation, appeals, and public verification status."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter a MemeTokenHub platform JWT."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    string xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

builder.Services.AddOptions<MongoOptions>()
    .Bind(builder.Configuration.GetSection(MongoOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ServiceEndpointOptions>()
    .Bind(builder.Configuration.GetSection(ServiceEndpointOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<AttachmentOptions>()
    .Bind(builder.Configuration.GetSection(AttachmentOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

MongoOptions mongoOptions = builder.Configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>()
    ?? throw new InvalidOperationException("MongoDb configuration is required.");
MongoClient mongoClient = new(mongoOptions.ConnectionString);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(mongoClient.GetDatabase(mongoOptions.DatabaseName));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IClaimRepository, MongoClaimRepository>();
builder.Services.AddScoped<IAttachmentRepository, MongoAttachmentRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IReferenceValidationService, ReferenceValidationService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddSingleton<IEventEnvelopeFactory, ClaimEventEnvelopeFactory>();
builder.Services.AddTransient<ServiceAuthenticationHandler>();

ServiceEndpointOptions endpoints = builder.Configuration.GetSection(ServiceEndpointOptions.SectionName).Get<ServiceEndpointOptions>()
    ?? throw new InvalidOperationException("ServiceEndpoints configuration is required.");
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(endpoints.UserService);
    client.Timeout = TimeSpan.FromSeconds(5);
})
    .AddHttpMessageHandler<ServiceAuthenticationHandler>();
builder.Services.AddHttpClient("TokenService", client =>
{
    client.BaseAddress = new Uri(endpoints.TokenService);
    client.Timeout = TimeSpan.FromSeconds(5);
})
    .AddHttpMessageHandler<ServiceAuthenticationHandler>();

builder.Services.AddMemeTokenHubAuthentication(builder.Configuration);
builder.Services.AddAuthorizationBuilder().AddPolicy("ClaimModerator", policy =>
    policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.User.IsInRole("Moderator") || context.User.HasClaim("capability", "moderation:claims")));
builder.Services.AddAuthorizationBuilder().AddPolicy("AttachmentScanner", policy =>
    policy.RequireAuthenticatedUser().RequireClaim("capability", "attachments:scan"));

ServiceBusOptions serviceBusOptions = builder.Configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
    ?? throw new InvalidOperationException("ServiceBus configuration is required.");
IHealthChecksBuilder healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("application", () => HealthCheckResult.Healthy("Claim Service is running."), tags: ["live", "ready", "dashboard"])
    .AddMongoDb(
        _ => mongoClient,
        name: "mongodb",
        tags: ["ready", "dashboard"],
        timeout: TimeSpan.FromSeconds(6));
healthChecks.Add(new HealthCheckRegistration(
    "user-service",
    provider => new DownstreamServiceHealthCheck(provider.GetRequiredService<IHttpClientFactory>(), "UserService"),
    HealthStatus.Unhealthy,
    ["ready", "dashboard"],
    TimeSpan.FromSeconds(6)));
healthChecks.Add(new HealthCheckRegistration(
    "token-service",
    provider => new DownstreamServiceHealthCheck(provider.GetRequiredService<IHttpClientFactory>(), "TokenService"),
    HealthStatus.Unhealthy,
    ["ready", "dashboard"],
    TimeSpan.FromSeconds(6)));
if (serviceBusOptions.Enabled)
{
    builder.Services.AddSingleton(new ServiceBusClient(serviceBusOptions.ConnectionString));
    builder.Services.AddSingleton(provider => provider.GetRequiredService<ServiceBusClient>().CreateSender(serviceBusOptions.TopicName));
    builder.Services.AddHostedService<MongoIndexInitializer>();
    builder.Services.AddHostedService<OutboxPublisher>();
    healthChecks.AddCheck<ServiceBusHealthCheck>("azure-service-bus", tags: ["ready", "dashboard"], timeout: TimeSpan.FromSeconds(6));
}

WebApplication app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MemeTokenHub Claim Service v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("dashboard"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.Run();

public partial class Program;
