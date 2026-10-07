namespace ClaimSettlement.Infrastructure;

/// <summary>
/// Configuration for the Policy Admin System HTTP client. The base address is
/// supplied through configuration (appsettings / environment variables) and must
/// not be hard-coded.
/// </summary>
public sealed class PolicyAdminOptions
{
    public const string SectionName = "PolicyAdmin";

    /// <summary>Base address of the Policy Admin System, e.g. https://policy-admin.example.com .</summary>
    public string BaseAddress { get; set; } = string.Empty;
}
