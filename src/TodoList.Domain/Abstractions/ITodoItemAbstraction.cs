using TodoList.Domain.Entities;

namespace TodoList.Domain.Abstractions;

public interface ITodoItemAbstraction
{
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TodoItem>> GetByListIdAsync(int todoListId, CancellationToken cancellationToken = default);
    Task<TodoList.Domain.PaginatedResult<TodoItem>> GetFilteredAsync(bool? isInMyDay, bool? hasDueDate, bool? isImportant, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
