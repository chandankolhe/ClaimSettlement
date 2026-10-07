namespace ClaimSettlement.Domain.Models;

/// <summary>
/// HTTP response contract as mandated by the brief. Field names and meaning must not change.
/// </summary>
public record SettlementDecision(
    string PolicyNumber,
    bool IsApproved,
    string StatusReason);
