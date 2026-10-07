using ClaimSettlement.Domain.Models;

namespace ClaimSettlement.Application.Interfaces;

/// <summary>
/// Abstraction over decision persistence. Keeps the business rules independent
/// of any particular storage technology. An in-memory implementation is
/// sufficient for this exercise.
/// </summary>
public interface IDecisionStore
{
    /// <summary>Records a settlement decision that has been produced after policy retrieval and rule evaluation.</summary>
    Task RecordAsync(SettlementDecision decision, CancellationToken cancellationToken);
}
