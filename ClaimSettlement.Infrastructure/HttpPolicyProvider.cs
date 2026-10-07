using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace ClaimSettlement.Infrastructure;

/// <summary>
/// HTTP implementation of <see cref="IPolicyProvider"/> that talks to the Policy
/// Admin System. Translates transport outcomes into domain outcomes:
/// 404 -> null (not found), success -> <see cref="PolicyDetails"/>, anything
/// else (including network failures) -> <see cref="PolicyProviderException"/>.
/// </summary>
public sealed class HttpPolicyProvider : IPolicyProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpPolicyProvider> _logger;

    public HttpPolicyProvider(HttpClient httpClient, ILogger<HttpPolicyProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken)
    {
        var requestUri = $"policies/{Uri.EscapeDataString(policyNumber)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Do not log the policy number (potential PII) or any secrets.
            _logger.LogError(ex, "Policy Admin System request failed to complete.");
            throw new PolicyProviderException("The Policy Admin System is unavailable.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Policy Admin System returned an unexpected status code {StatusCode}.",
                    (int)response.StatusCode);
                throw new PolicyProviderException("The Policy Admin System returned an unsuccessful response.");
            }

            try
            {
                var payload = await response.Content.ReadFromJsonAsync<PolicyResponse>(SerializerOptions, cancellationToken);
                if (payload is null)
                {
                    throw new PolicyProviderException("The Policy Admin System returned an empty response body.");
                }

                return new PolicyDetails(payload.CoverageLimit, payload.IsActive);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Policy Admin System returned a response that could not be parsed.");
                throw new PolicyProviderException("The Policy Admin System returned an invalid response.", ex);
            }
        }
    }
}
