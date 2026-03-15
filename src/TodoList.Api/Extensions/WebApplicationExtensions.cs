using TodoList.Api.Middleware;

namespace TodoList.Api.Extensions;

public static class WebApplicationExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
