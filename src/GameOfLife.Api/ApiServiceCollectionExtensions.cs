using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using GameOfLife.Api.Configuration;
using GameOfLife.Api.ErrorHandling;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace GameOfLife.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // Validation error keys use JSON property names ("cells", not "Cells").
        services.AddControllers(o => o.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(o => o.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
        services.AddExceptionHandler<BadHttpRequestExceptionHandler>();

        services.AddOptions<ApiLimitsOptions>()
            .BindConfiguration(ApiLimitsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<KestrelServerOptions>()
            .Configure<IOptions<ApiLimitsOptions>>((kestrel, limits) =>
                kestrel.Limits.MaxRequestBodySize = limits.Value.MaxRequestBodyBytes);

        services.AddRequestTimeouts();
        services.AddOptions<RequestTimeoutOptions>()
            .Configure<IOptions<ApiLimitsOptions>>((timeouts, limits) =>
                timeouts.DefaultPolicy = new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(limits.Value.RequestTimeoutSeconds),
                    TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable,
                });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<ApiLimitsOptions>>().Value;
                var clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.RateLimitPermitLimit,
                    Window = TimeSpan.FromSeconds(limits.RateLimitWindowSeconds),
                    QueueLimit = 0,
                });
            });
            o.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
