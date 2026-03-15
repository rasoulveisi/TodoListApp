using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.Categories;

public record GetCategoryQuery(int Id) : IRequest<CategoryResponse?>;

public class GetCategoryQueryHandler(ICategoryAbstraction categories)
    : IRequestHandler<GetCategoryQuery, CategoryResponse?>
{
    public async Task<CategoryResponse?> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        return category is null ? null : new CategoryResponse(category.Id, category.Name, category.Color);
    }
}
