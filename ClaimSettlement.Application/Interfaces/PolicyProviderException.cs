namespace ClaimSettlement.Application.Interfaces;

/// <summary>
/// Raised when the Policy Admin System is unavailable or responds in an
/// unexpected or unsuccessful way. The API layer maps this to a generic
/// error response without leaking internal details to the caller.
/// </summary>
public sealed class PolicyProviderException : Exception
{
    public PolicyProviderException(string message) : base(message)
    {
    }

    public PolicyProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
