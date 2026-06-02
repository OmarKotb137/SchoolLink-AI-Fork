using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Project.Domain.Exceptions;
using Serilog;
using Serilog.Context;

namespace Project.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            Log.Error(ex, "Response already started - cannot handle exception properly");
            return;
        }

        context.Response.ContentType = "application/json";

        var (statusCode, errorCode, message) = ex switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                notFound.Message
            ),

            ValidationException validation => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                validation.Message
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "ACCESS_DENIED",
                "You do not have permission to perform this action."
            ),

            InvalidOperationException invOp when
                invOp.Message.Contains("concurrency") => (
                HttpStatusCode.Conflict,
                "CONFLICT",
                "The resource was modified by another request. Please retry."
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later."
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var correlationId = context.TraceIdentifier
            ?? Guid.NewGuid().ToString("N")[..16];

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ErrorCode", errorCode))
        {
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                Log.Error(ex,
                    "[{ErrorCode}] Unhandled exception | CorrelationId: {CorrelationId} | " +
                    "Path: {Path} | User: {User} | Method: {Method} | IP: {IP} | {Message}",
                    errorCode, correlationId,
                    context.Request.Path, context.User.Identity?.Name ?? "Anonymous",
                    context.Request.Method, context.Connection.RemoteIpAddress,
                    ex.Message);
            }
            else
            {
                Log.Warning(ex,
                    "[{ErrorCode}] Business exception | CorrelationId: {CorrelationId} | " +
                    "Path: {Path} | User: {User} | Method: {Method} | {Message}",
                    errorCode, correlationId,
                    context.Request.Path, context.User.Identity?.Name ?? "Anonymous",
                    context.Request.Method, ex.Message);
            }
        }

        var response = new ErrorResponse
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Details = _env.IsDevelopment() ? new ErrorDetails
            {
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message
            } : null
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = _env.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ErrorDetails? Details { get; set; }
}

public class ErrorDetails
{
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}
