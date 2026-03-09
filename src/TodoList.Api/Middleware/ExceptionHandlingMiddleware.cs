using System.Text.Json;

namespace TodoList.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            var (statusCode, json) = GetErrorResponse(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static (int statusCode, string json) GetErrorResponse(Exception ex)
    {
        var name = ex.GetType().Name;
        var notFoundSuffix = "NotFound" + "Exception";
        var validationSuffix = "Validation" + "Exception";
        if (name.EndsWith(notFoundSuffix))
            return (404, JsonSerializer.Serialize(new { type = "NotFound", message = ex.Message }));
        if (name.EndsWith(validationSuffix))
        {
            var errorsProp = ex.GetType().GetProperty("Errors");
            var errors = errorsProp?.GetValue(ex);
            return (422, JsonSerializer.Serialize(new { type = "Validation", message = ex.Message, errors }));
        }
        return (500, JsonSerializer.Serialize(new { type = "Error", message = "An unexpected error occurred." }));
    }
}
