using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Repositories;

namespace TodoList.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TodoListDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITodoItemAbstraction, TodoItemRepository>();
        services.AddScoped<ITodoListAbstraction, TodoListRepository>();
        services.AddScoped<ICategoryAbstraction, CategoryRepository>();

        return services;
    }
}
