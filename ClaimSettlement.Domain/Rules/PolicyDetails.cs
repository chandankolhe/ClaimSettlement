namespace ClaimSettlement.Domain.Rules;

/// <summary>
/// Policy data retrieved from the Policy Admin System. This is the core domain
/// view of a policy and is intentionally decoupled from any transport/DTO shape.
/// </summary>
public sealed record PolicyDetails(decimal CoverageLimit, bool IsActive);
