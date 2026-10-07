using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Application.Services;
using ClaimSettlement.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind and validate Policy Admin configuration. The base address is required and
// supplied through configuration (never hard-coded or committed as a secret).
builder.Services
    .AddOptions<PolicyAdminOptions>()
    .Bind(builder.Configuration.GetSection(PolicyAdminOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseAddress), "PolicyAdmin:BaseAddress must be configured.")
    .ValidateOnStart();

// Typed HttpClient for the Policy Admin System. Using IHttpClientFactory gives us
// pooled, correctly-managed handlers and a natural seam for resilience policies.
builder.Services.AddHttpClient<IPolicyProvider, HttpPolicyProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PolicyAdminOptions>>().Value;
    client.BaseAddress = new Uri(AppendTrailingSlash(options.BaseAddress));
});

// In-memory decision store, shared as a singleton for the lifetime of the app.
builder.Services.AddSingleton<InMemoryDecisionStore>();
builder.Services.AddSingleton<IDecisionStore>(sp => sp.GetRequiredService<InMemoryDecisionStore>());

builder.Services.AddScoped<IClaimSettlementService, ClaimSettlementService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

static string AppendTrailingSlash(string baseAddress) =>
    baseAddress.EndsWith('/') ? baseAddress : baseAddress + "/";

// Exposed so the integration tests can reference the entry-point assembly via WebApplicationFactory.
public partial class Program;
