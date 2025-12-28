using System.Net;
using System.Text.Json;
using EMR.Application.Common.Exceptions;
using EMR.Domain.Exceptions;
using FluentValidation;

namespace EMR.Api.Middleware;

/// <summary>
/// Global exception handler middleware for HIPAA-compliant error handling.
/// Prevents stack trace exposure and PII leakage in error responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    // PII patterns to redact from error messages
    private static readonly string[] PiiPatterns = new[]
    {
        @"\b\d{3}-\d{2}-\d{4}\b", // SSN
        @"\b\d{9}\b", // SSN without dashes
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", // Email
        @"\b\d{10}\b", // Phone number
        @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", // Phone with separators
    };

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;

        // Log the full exception with correlation ID for debugging
        _logger.LogError(
            exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId,
            context.Request.Path,
            context.Request.Method);

        // Determine the appropriate response
        var (statusCode, errorCode, message) = MapExceptionToResponse(exception);

        // Build error response without exposing sensitive details
        var errorResponse = new ErrorResponse
        {
            Error = errorCode,
            Message = message,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow
        };

        // In development, include additional debugging info (but still redact PII)
        if (_environment.IsDevelopment())
        {
            errorResponse.Details = RedactPii(exception.Message);
            errorResponse.ExceptionType = exception.GetType().Name;
        }

        // Ensure response hasn't started
        if (!context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            await context.Response.WriteAsJsonAsync(errorResponse, options);
        }
    }

    /// <summary>
    /// Map exception types to HTTP status codes and error messages.
    /// Returns generic messages to prevent information disclosure.
    /// </summary>
    private static (HttpStatusCode StatusCode, string ErrorCode, string Message) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            // Domain exceptions - client errors
            EntityNotFoundException => (
                HttpStatusCode.NotFound,
                "RESOURCE_NOT_FOUND",
                "The requested resource was not found."),

            BusinessRuleViolationException ex => (
                HttpStatusCode.BadRequest,
                "BUSINESS_RULE_VIOLATION",
                ex.Message), // Business rule messages are intentionally user-facing

            DomainException ex => (
                HttpStatusCode.BadRequest,
                "DOMAIN_ERROR",
                ex.Message), // Domain exception messages are intentionally user-facing

            // Validation exceptions
            FluentValidation.ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),

            DuplicateEntityException => (
                HttpStatusCode.Conflict,
                "DUPLICATE_ENTITY",
                "A resource with the same identifier already exists."),

            // Authorization exceptions
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "ACCESS_DENIED",
                "You do not have permission to perform this action."),

            // Cancellation
            OperationCanceledException => (
                HttpStatusCode.BadRequest,
                "REQUEST_CANCELLED",
                "The request was cancelled."),

            // Database exceptions (hide details) - order matters: specific before general
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "CONCURRENCY_ERROR",
                "The resource was modified by another request. Please refresh and try again."),

            Microsoft.EntityFrameworkCore.DbUpdateException => (
                HttpStatusCode.InternalServerError,
                "DATABASE_ERROR",
                "A database error occurred. Please try again later."),

            // Generic exceptions - never expose internal details
            ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "INVALID_REQUEST",
                "The request was invalid."),

            ArgumentException => (
                HttpStatusCode.BadRequest,
                "INVALID_ARGUMENT",
                "One or more arguments were invalid."),

            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "INVALID_OPERATION",
                "The requested operation is not valid in the current state."),

            TimeoutException => (
                HttpStatusCode.GatewayTimeout,
                "TIMEOUT",
                "The request timed out. Please try again."),

            // Default - internal server error with generic message
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later.")
        };
    }

    /// <summary>
    /// Redact PII patterns from error messages to prevent data leakage.
    /// </summary>
    private static string RedactPii(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var redacted = message;
        foreach (var pattern in PiiPatterns)
        {
            redacted = System.Text.RegularExpressions.Regex.Replace(
                redacted,
                pattern,
                "[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromMilliseconds(100)); // Timeout to prevent ReDoS
        }

        return redacted;
    }
}

/// <summary>
/// Standard error response structure for API errors.
/// </summary>
public class ErrorResponse
{
    public required string Error { get; set; }
    public required string Message { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Additional details (only included in development environment).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Exception type name (only included in development environment).
    /// </summary>
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Extension methods for registering global exception handler middleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
