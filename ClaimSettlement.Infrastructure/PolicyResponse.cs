namespace ClaimSettlement.Infrastructure;

/// <summary>
/// Transport DTO mirroring the Policy Admin System JSON response:
/// <c>{ "coverageLimit": 10000, "isActive": true }</c>.
/// Kept internal to the infrastructure layer and mapped to the domain model.
/// </summary>
internal sealed record PolicyResponse(decimal CoverageLimit, bool IsActive);
