using ClaimSettlement.Application.Services;
using ClaimSettlement.Domain.Models;

namespace ClaimSettlement.Application.Interfaces;

/// <summary>
/// Coordinates a single claim-settlement request: retrieve policy data, apply the
/// business rules, and record the resulting decision. Holds no transport concerns
/// and delegates all rule logic to the pure domain layer.
/// </summary>
public interface IClaimSettlementService
{
    Task<ClaimEvaluationResult> EvaluateAsync(ClaimRequest request, CancellationToken cancellationToken);
}
