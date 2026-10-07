using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Domain.Models;
using ClaimSettlement.Domain.Rules;

namespace ClaimSettlement.Application.Services;

/// <inheritdoc />
public sealed class ClaimSettlementService : IClaimSettlementService
{
    private readonly IPolicyProvider _policyProvider;
    private readonly IDecisionStore _decisionStore;

    public ClaimSettlementService(IPolicyProvider policyProvider, IDecisionStore decisionStore)
    {
        _policyProvider = policyProvider;
        _decisionStore = decisionStore;
    }

    public async Task<ClaimEvaluationResult> EvaluateAsync(ClaimRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var policy = await _policyProvider.GetPolicyAsync(request.PolicyNumber, cancellationToken);
        if (policy is null)
        {
            // Policy not found is a dependency outcome, not a settlement decision:
            // nothing is evaluated and nothing is recorded.
            return ClaimEvaluationResult.PolicyNotFound();
        }

        var decision = SettlementRules.Evaluate(
            request.PolicyNumber,
            request.ClaimAmount,
            request.PropertyAgeYears,
            policy);

        // Only decisions produced after successful retrieval and evaluation are recorded.
        await _decisionStore.RecordAsync(decision, cancellationToken);

        return ClaimEvaluationResult.Decided(decision);
    }
}
