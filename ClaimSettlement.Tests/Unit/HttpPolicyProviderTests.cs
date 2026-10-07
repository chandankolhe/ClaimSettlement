using System.Net;
using System.Text;
using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClaimSettlement.Tests.Unit;

/// <summary>
/// Tests that the HTTP policy provider maps transport outcomes to domain outcomes:
/// 200 -> details, 404 -> null, 5xx/invalid/network -> PolicyProviderException.
/// </summary>
public class HttpPolicyProviderTests
{
    private static HttpPolicyProvider CreateProvider(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://policy-admin.test/") };
        return new HttpPolicyProvider(client, NullLogger<HttpPolicyProvider>.Instance);
    }

    [Fact]
    public async Task GetPolicyAsync_OnSuccess_ReturnsMappedDetails()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{ "coverageLimit": 10000, "isActive": true }"""));
        var provider = CreateProvider(handler);

        var result = await provider.GetPolicyAsync("POL-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.CoverageLimit.Should().Be(10000);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetPolicyAsync_OnNotFound_ReturnsNull()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var provider = CreateProvider(handler);

        var result = await provider.GetPolicyAsync("MISSING", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_OnServerError_Throws()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);

        var act = () => provider.GetPolicyAsync("POL-1", CancellationToken.None);

        await act.Should().ThrowAsync<PolicyProviderException>();
    }

    [Fact]
    public async Task GetPolicyAsync_OnInvalidJson_Throws()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, "not-json"));
        var provider = CreateProvider(handler);

        var act = () => provider.GetPolicyAsync("POL-1", CancellationToken.None);

        await act.Should().ThrowAsync<PolicyProviderException>();
    }

    [Fact]
    public async Task GetPolicyAsync_OnNetworkFailure_Throws()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));
        var provider = CreateProvider(handler);

        var act = () => provider.GetPolicyAsync("POL-1", CancellationToken.None);

        await act.Should().ThrowAsync<PolicyProviderException>();
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
