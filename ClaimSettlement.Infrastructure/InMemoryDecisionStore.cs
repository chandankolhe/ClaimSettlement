using System.Collections.Concurrent;
using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Domain.Models;

namespace ClaimSettlement.Infrastructure;

/// <summary>
/// Thread-safe in-memory <see cref="IDecisionStore"/>. Sufficient for this
/// exercise; a durable store would replace this behind the same abstraction.
/// </summary>
public sealed class InMemoryDecisionStore : IDecisionStore
{
    private readonly ConcurrentQueue<SettlementDecision> _decisions = new();

    public Task RecordAsync(SettlementDecision decision, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _decisions.Enqueue(decision);
        return Task.CompletedTask;
    }

    /// <summary>All recorded decisions, in the order they were recorded. Intended for tests and diagnostics.</summary>
    public IReadOnlyCollection<SettlementDecision> Decisions => _decisions.ToArray();
}
