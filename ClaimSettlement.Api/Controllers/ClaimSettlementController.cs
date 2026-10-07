using ClaimSettlement.Api.Validations;
using ClaimSettlement.Application.Interfaces;
using ClaimSettlement.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClaimSettlement.Api.Controllers;

/// <summary>
/// Handles POST /claim-settlement. Owns HTTP concerns only: validation,
/// status-code mapping, and delegation to <see cref="IClaimSettlementService"/>.
/// </summary>
[ApiController]
[Route("claim-settlement")]
public class ClaimSettlementController : ControllerBase
{
    private readonly IClaimSettlementService _settlementService;
    private readonly ILogger<ClaimSettlementController> _logger;

    public ClaimSettlementController(
        IClaimSettlementService settlementService,
        ILogger<ClaimSettlementController> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SettlementDecision), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Post(
        [FromBody] ClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!ClaimRequestValidator.TryValidate(request, out var errors))
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        try
        {
            var result = await _settlementService.EvaluateAsync(request, cancellationToken);

            if (!result.PolicyFound)
            {
                return Problem(
                    title: "Policy not found",
                    detail: "No policy was found for the supplied policy number.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(result.Decision);
        }
        catch (PolicyProviderException ex)
        {
            // Log the detail internally; return a generic message to the caller.
            _logger.LogError(ex, "Claim settlement failed because of a policy dependency error.");
            return Problem(
                title: "Policy dependency unavailable",
                detail: "The policy service is currently unavailable. Please try again later.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or the request was cancelled; stop doing work.
            // 499 (Client Closed Request) is a non-standard but widely understood code for this case.
            return StatusCode(499);
        }
    }
}
