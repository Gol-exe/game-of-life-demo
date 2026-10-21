using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.ErrorHandling;

/// <summary>
/// Preserves the status code of <see cref="BadHttpRequestException"/> (e.g. 413 when the body exceeds
/// the configured limit) instead of letting it surface as a 500. Other exceptions fall through to the
/// default handler, which returns a generic 500 problem response without leaking details.
/// </summary>
internal sealed class BadHttpRequestExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        httpContext.Response.StatusCode = badRequest.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "Request body too large." : "Bad request.",
            },
        });
    }
}
