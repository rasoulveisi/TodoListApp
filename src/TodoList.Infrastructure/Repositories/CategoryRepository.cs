using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Abstractions;
using TodoList.Domain.Entities;

namespace TodoList.Infrastructure.Repositories;

public class CategoryRepository(TodoListDbContext context) : ICategoryAbstraction
{
    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Categories.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await context.Categories.AddAsync(category, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category != null)
        {
            context.Categories.Remove(category);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
