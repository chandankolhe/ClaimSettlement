using ClaimSettlement.Domain.Rules;

namespace ClaimSettlement.Application.Interfaces;

/// <summary>
/// Abstraction over the Policy Admin System. Keeps transport concerns (HTTP,
/// serialization, status codes) out of the core decision logic and enables
/// deterministic testing via test doubles.
/// </summary>
public interface IPolicyProvider
{
    /// <summary>
    /// Retrieves policy details for the given policy number.
    /// Returns <c>null</c> when the policy does not exist (a dependency outcome,
    /// not a settlement decision).
    /// </summary>
    /// <exception cref="PolicyProviderException">
    /// Thrown when the dependency is unavailable or returns an unexpected/unsuccessful response.
    /// </exception>
    Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken);
}
