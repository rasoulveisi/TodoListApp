namespace Test.Api.Tests
{
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using System.Net;
    using System.Net.Http.Json;
    using System.Linq;
    using System.Text.Json;
    using TodoList.Infrastructure;

    public class IntegrationTest1
    {
        [Fact]
        public async Task TaskApiFlow_Create_Retrieve_Complete_Filter_ShouldReturnExpectedResults_And_ValidationErrors()
        {
            await using var app = new IsolatedTodoListApiFactory();
            using var client = app.CreateClient();
            var createPayload = new
            {
                Title = "Buy groceries",
                Description = "Milk and bread",
                TodoListId = (int?)null,
                DueDate = (DateTime?)new DateTime(2026, 9, 10),
                IsImportant = true,
                IsInMyDay = true,
                Recurrence = (object?)null,
                CategoryIds = Array.Empty<int>()
            };

            using var createResponse = await client.PostAsJsonAsync("/api/todoitems", createPayload);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = await createResponse.Content.ReadFromJsonAsync<TodoItemResponse>(JsonOptions.Instance);
            Assert.NotNull(created);
            Assert.Equal(createPayload.Title, created!.Title);
            Assert.False(created.IsCompleted);

            using var getResponse = await client.GetAsync($"/api/todoitems/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var fetched = await getResponse.Content.ReadFromJsonAsync<TodoItemResponse>(JsonOptions.Instance);
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched!.Id);

            using var completeResponse = await client.PostAsJsonAsync($"/api/todoitems/{created.Id}/complete", new { complete = true });
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
            var completed = await completeResponse.Content.ReadFromJsonAsync<TodoItemResponse>(JsonOptions.Instance);
            Assert.NotNull(completed);
            Assert.True(completed!.IsCompleted);

            using var filteredResponse = await client.GetAsync("/api/todoitems?isInMyDay=true&page=1&pageSize=5");
            Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
            var filtered = await filteredResponse.Content.ReadFromJsonAsync<PaginatedTodoItemsResponse>(JsonOptions.Instance);
            Assert.NotNull(filtered);
            Assert.Contains(filtered!.Items, item => item.Id == created.Id);

            using var missingItemResponse = await client.GetAsync("/api/todoitems/999999");
            Assert.Equal(HttpStatusCode.NotFound, missingItemResponse.StatusCode);

            using var invalidPaginationResponse = await client.GetAsync("/api/todoitems?page=0&pageSize=500");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidPaginationResponse.StatusCode);
        }

        private sealed class IsolatedTodoListApiFactory : WebApplicationFactory<Program>
        {
            private readonly string _databaseName = Guid.NewGuid().ToString("N");

            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.ConfigureServices(services =>
                {
                    var existingContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TodoListDbContext>));
                    if (existingContextDescriptor is not null)
                        services.Remove(existingContextDescriptor);

                    services.RemoveAll<IDbContextOptionsConfiguration<TodoListDbContext>>();

                    services.AddDbContext<TodoListDbContext>(options =>
                        options.UseInMemoryDatabase(_databaseName));
                });

                var host = base.CreateHost(builder);
                using var scope = host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TodoListDbContext>();
                context.Database.EnsureCreated();
                return host;
            }
        }

        private static class JsonOptions
        {
            public static readonly JsonSerializerOptions Instance = new() { PropertyNameCaseInsensitive = true };
        }

        private sealed record TodoItemResponse(
            int Id,
            string Title,
            string? Description,
            bool IsCompleted,
            DateTime? DueDate,
            bool IsImportant,
            bool IsInMyDay,
            DateTime CreatedAt,
            DateTime? CompletedAt,
            object? Recurrence,
            int? TodoListId,
            IReadOnlyList<object> Categories);

        private sealed record PaginatedTodoItemsResponse(
            IReadOnlyList<TodoItemResponse> Items,
            int TotalCount,
            int Page,
            int PageSize,
            int TotalPages);
    }
}
