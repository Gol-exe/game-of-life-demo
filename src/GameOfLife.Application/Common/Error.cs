namespace GameOfLife.Application.Common;

public enum ErrorType
{
    /// <summary>The request is malformed or breaks a limit.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The request is valid but cannot be fulfilled for this resource.</summary>
    Unprocessable,
}

/// <summary>An expected, client-facing failure.</summary>
/// <param name="Code">A stable, machine-readable error code.</param>
/// <param name="Message">A human-readable description.</param>
/// <param name="Type">The category of failure.</param>
/// <param name="Target">The input the error relates to, for validation errors.</param>
public sealed record Error(string Code, string Message, ErrorType Type, string? Target = null);
