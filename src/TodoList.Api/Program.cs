using TodoList.Api.Extensions;
using TodoList.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandling();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Content(
        """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8"/>
            <title>TodoList API</title>
            <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"/>
        </head>
        <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
            <script>
                SwaggerUIBundle({
                    url: "/openapi/v1.json",
                    dom_id: "#swagger-ui"
                });
            </script>
        </body>
        </html>
        """, "text/html"));
}

app.Run();
