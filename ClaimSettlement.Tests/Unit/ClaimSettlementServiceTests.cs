using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Application.Services;
using ClaimSettlement.Domain.Models;
using ClaimSettlement.Domain.Rules;
using ClaimSettlement.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ClaimSettlement.Tests.Unit;

/// <summary>
/// Tests for the orchestrating service: recording behaviour, policy-not-found
/// handling, and cancellation propagation. Uses lightweight hand-written fakes
/// so the tests are deterministic and free of infrastructure.
/// </summary>
public class ClaimSettlementServiceTests
{
    [Fact]
    public async Task EvaluateAsync_WhenPolicyFound_RecordsDecision()
    {
        var provider = new FakePolicyProvider(new PolicyDetails(CoverageLimit: 10000, IsActive: true));
        var store = new InMemoryDecisionStore();
        var service = new ClaimSettlementService(provider, store);

        var result = await service.EvaluateAsync(new ClaimRequest("POL-1", 2500, 10), CancellationToken.None);

        result.PolicyFound.Should().BeTrue();
        result.Decision!.IsApproved.Should().BeTrue();
        store.Decisions.Should().ContainSingle()
            .Which.StatusReason.Should().Be(StatusReasons.AutoSettled);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPolicyNotFound_DoesNotRecordDecision()
    {
        var provider = new FakePolicyProvider(policy: null);
        var store = new InMemoryDecisionStore();
        var service = new ClaimSettlementService(provider, store);

        var result = await service.EvaluateAsync(new ClaimRequest("MISSING", 2500, 10), CancellationToken.None);

        result.PolicyFound.Should().BeFalse();
        result.Decision.Should().BeNull();
        store.Decisions.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_WhenCancelled_DoesNotCallProviderAndDoesNotRecord()
    {
        var provider = new FakePolicyProvider(new PolicyDetails(10000, true));
        var store = new InMemoryDecisionStore();
        var service = new ClaimSettlementService(provider, store);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.EvaluateAsync(new ClaimRequest("POL-1", 2500, 10), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        provider.CallCount.Should().Be(0);
        store.Decisions.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_WhenDependencyFails_PropagatesExceptionAndDoesNotRecord()
    {
        var provider = new ThrowingPolicyProvider();
        var store = new InMemoryDecisionStore();
        var service = new ClaimSettlementService(provider, store);

        var act = () => service.EvaluateAsync(new ClaimRequest("POL-1", 2500, 10), CancellationToken.None);

        await act.Should().ThrowAsync<PolicyProviderException>();
        store.Decisions.Should().BeEmpty();
    }

    private sealed class FakePolicyProvider : IPolicyProvider
    {
        private readonly PolicyDetails? _policy;

        public FakePolicyProvider(PolicyDetails? policy) => _policy = policy;

        public int CallCount { get; private set; }

        public Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(_policy);
        }
    }

    private sealed class ThrowingPolicyProvider : IPolicyProvider
    {
        public Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken)
            => throw new PolicyProviderException("boom");
    }
}
