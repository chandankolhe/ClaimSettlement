using ClaimSettlement.Domain.Models;

namespace ClaimSettlement.Api.Validations;

/// <summary>
/// Minimal boundary validation for <see cref="ClaimRequest"/> as required by the brief.
/// </summary>
public static class ClaimRequestValidator
{
    public static bool TryValidate(ClaimRequest? request, out IDictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors[nameof(ClaimRequest)] = new[] { "A request body is required." };
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.PolicyNumber))
        {
            errors[nameof(ClaimRequest.PolicyNumber)] = new[] { "PolicyNumber must not be blank." };
        }

        if (request.ClaimAmount <= 0)
        {
            errors[nameof(ClaimRequest.ClaimAmount)] = new[] { "ClaimAmount must be greater than zero." };
        }

        if (request.PropertyAgeYears < 0)
        {
            errors[nameof(ClaimRequest.PropertyAgeYears)] = new[] { "PropertyAgeYears must not be negative." };
        }

        return errors.Count == 0;
    }
}
