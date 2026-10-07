using System.Net;
using System.Net.Http.Json;
using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Domain.Models;
using ClaimSettlement.Domain.Rules;
using FluentAssertions;
using Xunit;

namespace ClaimSettlement.Tests.Integration;

/// <summary>
/// End-to-end tests through the HTTP boundary: validation mapping, the auto-settled
/// happy path, a non-approval path, policy-not-found, and dependency failure.
/// </summary>
public class ClaimSettlementApiTests : IClassFixture<ClaimSettlementApiFactory>
{
    private readonly ClaimSettlementApiFactory _factory;

    public ClaimSettlementApiTests(ClaimSettlementApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_AutoSettledClaim_Returns200WithApprovedDecision()
    {
        _factory.PolicyProvider.Policy = new PolicyDetails(CoverageLimit: 10000, IsActive: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/claim-settlement", new ClaimRequest("POL-1", 2500, 10));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decision = await response.Content.ReadFromJsonAsync<SettlementDecision>();
        decision.Should().NotBeNull();
        decision!.IsApproved.Should().BeTrue();
        decision.StatusReason.Should().Be(StatusReasons.AutoSettled);
        decision.PolicyNumber.Should().Be("POL-1");
    }

    [Fact]
    public async Task Post_OverThresholdClaim_Returns200WithReviewReason()
    {
        _factory.PolicyProvider.Policy = new PolicyDetails(CoverageLimit: 10000, IsActive: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/claim-settlement", new ClaimRequest("POL-1", 3500, 10));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decision = await response.Content.ReadFromJsonAsync<SettlementDecision>();
        decision!.IsApproved.Should().BeFalse();
        decision.StatusReason.Should().Be(StatusReasons.AdjusterReviewRequired);
    }

    [Fact]
    public async Task Post_InvalidRequest_Returns400()
    {
        var client = _factory.CreateClient();

        // Blank policy number, non-positive amount, negative age.
        var response = await client.PostAsJsonAsync("/claim-settlement", new ClaimRequest("", 0, -1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_PolicyNotFound_Returns404()
    {
        _factory.PolicyProvider.Policy = null;
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/claim-settlement", new ClaimRequest("MISSING", 2500, 10));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_DependencyFailure_Returns502WithoutLeakingDetail()
    {
        _factory.PolicyProvider.Policy = null;
        _factory.PolicyProvider.ExceptionToThrow = new PolicyProviderException("internal connection string leaked here");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/claim-settlement", new ClaimRequest("POL-1", 2500, 10));

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("connection string");

        // Reset shared fixture state so other tests are unaffected.
        _factory.PolicyProvider.ExceptionToThrow = null;
    }
}
