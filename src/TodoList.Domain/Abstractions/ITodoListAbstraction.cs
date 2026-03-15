using TodoList.Domain.Entities;

namespace TodoList.Domain.Abstractions;

public interface ITodoListAbstraction
{
    Task<Entities.TodoList?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Entities.TodoList>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Entities.TodoList list, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.TodoList list, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
