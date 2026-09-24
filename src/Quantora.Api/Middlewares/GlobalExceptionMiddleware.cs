using Quantora.Application.Common;
using Quantora.Application.Exceptions;
using Quantora.Infrastructure.Broker.Upstox.Exceptions;
using System.Text.Json;

namespace Quantora.Api.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                context.TraceIdentifier);

            await HandleAsync(context, exception);
        }
    }

    private static async Task HandleAsync(
        HttpContext context,
        Exception exception)
    {
        var result = exception switch
        {
            AuthenticationException ex =>
                (Status: 401, Message: ex.Message, Errors: new[] { ex.Message }),

            ConflictException ex =>
                (Status: 409, Message: ex.Message, Errors: new[] { ex.Message }),

            NotFoundException ex =>
                (Status: 404, Message: ex.Message, Errors: new[] { ex.Message }),

            UpstoxApiException ex =>
                (Status: ex.StatusCode, Message: ex.Message, Errors: new[] { ex.Message }),

            ArgumentException ex =>
                (Status: 400, Message: ex.Message, Errors: new[] { ex.Message }),

            UnauthorizedAccessException ex =>
                (Status: 401, Message: ex.Message, Errors: new[] { ex.Message }),

            _ =>
                (Status: 500, Message: "An unexpected error occurred.", Errors: new[] { "Please try again later." })
        };

        context.Response.StatusCode = result.Status;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.FailureResponse(
            result.Message,
            result.Errors);

        response.TraceId = context.TraceIdentifier;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}
