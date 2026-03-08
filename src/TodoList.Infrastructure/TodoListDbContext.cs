using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Entities;

// using alias for TodoList entity to avoid conflicts with the namespace
using TodoListEntity = TodoList.Domain.Entities.TodoList;

namespace TodoList.Infrastructure;

public class TodoListDbContext(DbContextOptions<TodoListDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoListEntity> TodoLists => Set<TodoListEntity>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoListDbContext).Assembly);
    }
}
