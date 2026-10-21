using GameOfLife.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GameOfLife.Api.ErrorHandling;

/// <summary>Maps application <see cref="Error"/>s to RFC 7807 problem responses.</summary>
public static class ErrorResults
{
    public const string ErrorCodeExtension = "errorCode";

    public static ActionResult ToProblem(this ControllerBase controller, Error error)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);

        var factory = controller.ProblemDetailsFactory;
        var httpContext = controller.HttpContext;

        ProblemDetails problem = error.Type switch
        {
            ErrorType.Validation => CreateValidationProblem(factory, httpContext, error),
            ErrorType.NotFound => factory.CreateProblemDetails(httpContext, StatusCodes.Status404NotFound, "Resource not found.", detail: error.Message),
            ErrorType.Unprocessable => factory.CreateProblemDetails(httpContext, StatusCodes.Status422UnprocessableEntity, "Request could not be processed.", detail: error.Message),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type."),
        };

        problem.Extensions[ErrorCodeExtension] = error.Code;
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    private static ValidationProblemDetails CreateValidationProblem(ProblemDetailsFactory factory, HttpContext httpContext, Error error)
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(error.Target ?? string.Empty, error.Message);
        return factory.CreateValidationProblemDetails(httpContext, modelState, StatusCodes.Status400BadRequest);
    }
}
