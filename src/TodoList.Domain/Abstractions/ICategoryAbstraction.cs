using TodoList.Domain.Entities;

namespace TodoList.Domain.Abstractions;

public interface ICategoryAbstraction
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
