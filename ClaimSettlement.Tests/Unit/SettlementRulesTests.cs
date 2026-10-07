using ClaimSettlement.Domain.Rules;
using FluentAssertions;
using Xunit;

namespace ClaimSettlement.Tests.Unit;

/// <summary>
/// Unit tests for the pure business rules, including the brief's worked examples,
/// reason precedence, and the older-property threshold boundary.
/// </summary>
public class SettlementRulesTests
{
    // The five worked examples from the brief.
    [Theory]
    [InlineData(false, 10000, 500, 10, false, StatusReasons.PolicyInactive)]
    [InlineData(true, 1000, 1500, 10, false, StatusReasons.CoverageLimitExceeded)]
    [InlineData(true, 10000, 3500, 10, false, StatusReasons.AdjusterReviewRequired)]
    [InlineData(true, 10000, 1500, 35, false, StatusReasons.AdjusterReviewRequired)]
    [InlineData(true, 10000, 2500, 10, true, StatusReasons.AutoSettled)]
    public void Evaluate_MatchesBriefExamples(
        bool isActive,
        decimal coverageLimit,
        decimal claimAmount,
        int propertyAgeYears,
        bool expectedApproved,
        string expectedReason)
    {
        var policy = new PolicyDetails(coverageLimit, isActive);

        var decision = SettlementRules.Evaluate("POL-1", claimAmount, propertyAgeYears, policy);

        decision.IsApproved.Should().Be(expectedApproved);
        decision.StatusReason.Should().Be(expectedReason);
        decision.PolicyNumber.Should().Be("POL-1");
    }

    [Fact]
    public void Evaluate_InactivePolicy_TakesPrecedenceOverCoverage()
    {
        // Inactive AND over coverage -> inactive reason wins.
        var policy = new PolicyDetails(CoverageLimit: 1000, IsActive: false);

        var decision = SettlementRules.Evaluate("POL-1", claimAmount: 5000, propertyAgeYears: 10, policy);

        decision.IsApproved.Should().BeFalse();
        decision.StatusReason.Should().Be(StatusReasons.PolicyInactive);
    }

    [Fact]
    public void Evaluate_CoverageExceeded_TakesPrecedenceOverThreshold()
    {
        // Over coverage AND over threshold -> coverage reason wins.
        var policy = new PolicyDetails(CoverageLimit: 2000, IsActive: true);

        var decision = SettlementRules.Evaluate("POL-1", claimAmount: 5000, propertyAgeYears: 10, policy);

        decision.IsApproved.Should().BeFalse();
        decision.StatusReason.Should().Be(StatusReasons.CoverageLimitExceeded);
    }

    [Theory]
    [InlineData(30, 3000, true, StatusReasons.AutoSettled)]   // exactly 30 years, at standard threshold
    [InlineData(30, 3001, false, StatusReasons.AdjusterReviewRequired)]
    [InlineData(31, 1000, true, StatusReasons.AutoSettled)]   // over 30 years, at older threshold
    [InlineData(31, 1001, false, StatusReasons.AdjusterReviewRequired)]
    public void Evaluate_AppliesCorrectThresholdAtAgeBoundary(
        int propertyAgeYears,
        decimal claimAmount,
        bool expectedApproved,
        string expectedReason)
    {
        var policy = new PolicyDetails(CoverageLimit: 10000, IsActive: true);

        var decision = SettlementRules.Evaluate("POL-1", claimAmount, propertyAgeYears, policy);

        decision.IsApproved.Should().Be(expectedApproved);
        decision.StatusReason.Should().Be(expectedReason);
    }

    [Fact]
    public void Evaluate_ClaimEqualToCoverageLimit_IsCovered()
    {
        var policy = new PolicyDetails(CoverageLimit: 3000, IsActive: true);

        var decision = SettlementRules.Evaluate("POL-1", claimAmount: 3000, propertyAgeYears: 10, policy);

        decision.IsApproved.Should().BeTrue();
        decision.StatusReason.Should().Be(StatusReasons.AutoSettled);
    }
}
