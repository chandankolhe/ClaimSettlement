using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Domain.Rules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClaimSettlement.Tests.Integration;

/// <summary>
/// Spins up the real API pipeline (routing, validation, JSON, DI) but replaces the
/// Policy Admin System with a controllable in-process fake, so tests are deterministic
/// and never touch the network.
/// </summary>
public sealed class ClaimSettlementApiFactory : WebApplicationFactory<Program>
{
    public StubPolicyProvider PolicyProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the real typed HttpClient registration and substitute our stub.
            services.RemoveAll<IPolicyProvider>();
            services.AddSingleton<IPolicyProvider>(PolicyProvider);
        });
    }

    /// <summary>A configurable in-memory policy provider for integration tests.</summary>
    public sealed class StubPolicyProvider : IPolicyProvider
    {
        public PolicyDetails? Policy { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Policy);
        }
    }
}
