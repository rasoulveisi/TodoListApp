using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Abstractions;
using TodoList.Domain.Entities;
using TodoList.Infrastructure.Pagination;

namespace TodoList.Infrastructure.Repositories;

public class TodoItemRepository(TodoListDbContext context) : ITodoItemAbstraction
{
    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.TodoItems
            .Include(t => t.Categories)
            .Include(t => t.TodoList)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.TodoItems
            .Include(t => t.Categories)
            .Include(t => t.TodoList)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TodoItem>> GetByListIdAsync(int todoListId, CancellationToken cancellationToken = default)
    {
        return await context.TodoItems
            .Include(t => t.Categories)
            .Where(t => t.TodoListId == todoListId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Domain.PaginatedResult<TodoItem>> GetFilteredAsync(bool? isInMyDay, bool? hasDueDate, bool? isImportant, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.TodoItems
            .Include(t => t.Categories)
            .Include(t => t.TodoList)
            .AsQueryable();

        if (isInMyDay.HasValue)
            query = query.Where(t => t.IsInMyDay == isInMyDay.Value);

        if (hasDueDate.HasValue)
            query = hasDueDate.Value
                ? query.Where(t => t.DueDate != null)
                : query.Where(t => t.DueDate == null);

        if (isImportant.HasValue)
            query = query.Where(t => t.IsImportant == isImportant.Value);

        query = query.OrderBy(t => t.Id);
        return await query.ToPaginatedResultAsync(page, pageSize, cancellationToken);
    }

    public async Task AddAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        await context.TodoItems.AddAsync(item, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        context.TodoItems.Update(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.TodoItems.FindAsync([id], cancellationToken);
        if (item != null)
        {
            context.TodoItems.Remove(item);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
