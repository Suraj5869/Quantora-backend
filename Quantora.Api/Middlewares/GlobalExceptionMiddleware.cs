using FluentValidation;
using Quantora.Application.Modules.Authentication.Exceptions;
using Quantora.Shared.Responses;
using System.Net;
using System.Text.Json;

namespace Quantora.Api.Middlewares
{
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

                await HandleExceptionAsync(
                    context,
                    exception);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            var statusCode = exception switch
            {
                ValidationException =>
                    HttpStatusCode.BadRequest,

                AuthenticationException =>
                    HttpStatusCode.Conflict,

                _ =>
                    HttpStatusCode.InternalServerError
            };

            var errors = exception switch
            {
                ValidationException validationException =>
                    validationException.Errors
                        .Select(error => error.ErrorMessage),

                AuthenticationException authenticationException =>
                    new[] { authenticationException.Message },

                _ =>
                    new[] { "Please try again later." }
            };

            context.Response.StatusCode =
                (int)statusCode;

            context.Response.ContentType =
                "application/json";

            var response =
                ApiResponse<object>.FailureResponse(
                    exception is ValidationException
                        ? "Validation failed."
                        : exception is AuthenticationException
                            ? exception.Message
                            : "An unexpected error occurred.",
                    errors);

            response.TraceId =
                context.TraceIdentifier;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}
