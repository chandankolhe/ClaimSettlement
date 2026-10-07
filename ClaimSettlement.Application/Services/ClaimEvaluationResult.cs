using ClaimSettlement.Domain.Models;

namespace ClaimSettlement.Application.Services;

/// <summary>
/// Outcome of evaluating a claim. Distinguishes a produced settlement decision
/// from the dependency outcome of "policy not found", which the brief requires
/// be treated differently from a settlement decision.
/// </summary>
public sealed record ClaimEvaluationResult
{
    private ClaimEvaluationResult(bool policyFound, SettlementDecision? decision)
    {
        PolicyFound = policyFound;
        Decision = decision;
    }

    public bool PolicyFound { get; }

    public SettlementDecision? Decision { get; }

    public static ClaimEvaluationResult Decided(SettlementDecision decision) => new(true, decision);

    public static ClaimEvaluationResult PolicyNotFound() => new(false, null);
}
