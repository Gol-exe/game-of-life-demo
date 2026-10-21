using System.ComponentModel.DataAnnotations;

namespace GameOfLife.Api.Configuration;

/// <summary>HTTP-level protections. Bound from the "ApiLimits" configuration section.</summary>
public sealed class ApiLimitsOptions
{
    public const string SectionName = "ApiLimits";

    /// <summary>The maximum request body size in bytes, enforced by Kestrel.</summary>
    [Range(1_024, 1_073_741_824)]
    public long MaxRequestBodyBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>Requests running longer than this are cancelled. Not enforced while a debugger is attached.</summary>
    [Range(1, 3_600)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>Requests allowed per client IP address in each rate-limit window.</summary>
    [Range(1, 1_000_000)]
    public int RateLimitPermitLimit { get; set; } = 100;

    [Range(1, 86_400)]
    public int RateLimitWindowSeconds { get; set; } = 60;
}
