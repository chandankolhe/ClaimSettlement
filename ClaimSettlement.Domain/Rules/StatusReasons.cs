namespace ClaimSettlement.Domain.Rules;

/// <summary>
/// The canonical status reasons returned by the settlement rules, in precedence order.
/// Centralised to avoid magic strings and keep the contract stable.
/// </summary>
public static class StatusReasons
{
    public const string PolicyInactive = "Policy Inactive";
    public const string CoverageLimitExceeded = "Coverage Limit Exceeded";
    public const string AdjusterReviewRequired = "Adjuster Review Required";
    public const string AutoSettled = "Auto-Settled";
}
