using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Abstractions;
using TodoListEntity = TodoList.Domain.Entities.TodoList;

namespace TodoList.Infrastructure.Repositories;

public class TodoListRepository(TodoListDbContext context) : ITodoListAbstraction
{
    public async Task<TodoListEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<List<TodoListEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.TodoLists
            .Include(l => l.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TodoListEntity list, CancellationToken cancellationToken = default)
    {
        await context.TodoLists.AddAsync(list, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TodoListEntity list, CancellationToken cancellationToken = default)
    {
        context.TodoLists.Update(list);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var list = await context.TodoLists.FindAsync([id], cancellationToken);
        if (list != null)
        {
            context.TodoLists.Remove(list);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
