namespace ClaimSettlement.Domain.Models;

/// <summary>
/// HTTP request contract as mandated by the brief. Field names and meaning must not change.
/// </summary>
public record ClaimRequest(
    string PolicyNumber,
    decimal ClaimAmount,
    int PropertyAgeYears);
