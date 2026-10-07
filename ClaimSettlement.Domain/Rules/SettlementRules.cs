using ClaimSettlement.Domain.Models;

namespace ClaimSettlement.Domain.Rules;

/// <summary>
/// Pure, infrastructure-free settlement rules. Given a claim and the policy
/// details retrieved from the Policy Admin System, this produces the settlement
/// decision following the reason precedence defined in the brief.
/// </summary>
public static class SettlementRules
{
    /// <summary>Automatic-settlement threshold for a property aged 30 years or less.</summary>
    public const decimal StandardThreshold = 3000m;

    /// <summary>Automatic-settlement threshold for a property aged over 30 years.</summary>
    public const decimal OlderPropertyThreshold = 1000m;

    /// <summary>The property age (in years) at or below which the standard threshold applies.</summary>
    public const int OlderPropertyAgeYears = 30;

    /// <summary>
    /// Evaluates a claim against policy details and returns the settlement decision.
    /// Reason precedence: inactive policy, then coverage, then auto-settlement threshold,
    /// otherwise auto-settled.
    /// </summary>
    public static SettlementDecision Evaluate(string policyNumber, decimal claimAmount, int propertyAgeYears, PolicyDetails policy)
    {
        if (!policy.IsActive)
        {
            return new SettlementDecision(policyNumber, false, StatusReasons.PolicyInactive);
        }

        if (claimAmount > policy.CoverageLimit)
        {
            return new SettlementDecision(policyNumber, false, StatusReasons.CoverageLimitExceeded);
        }

        var threshold = propertyAgeYears > OlderPropertyAgeYears
            ? OlderPropertyThreshold
            : StandardThreshold;

        if (claimAmount > threshold)
        {
            return new SettlementDecision(policyNumber, false, StatusReasons.AdjusterReviewRequired);
        }

        return new SettlementDecision(policyNumber, true, StatusReasons.AutoSettled);
    }
}
