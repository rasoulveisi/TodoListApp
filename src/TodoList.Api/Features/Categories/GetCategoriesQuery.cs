using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.Categories;

public record GetCategoriesQuery : IRequest<List<CategoryResponse>>;

public class GetCategoriesQueryHandler(ICategoryAbstraction categories)
    : IRequestHandler<GetCategoriesQuery, List<CategoryResponse>>
{
    public async Task<List<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var list = await categories.GetAllAsync(cancellationToken);
        return list.Select(c => new CategoryResponse(c.Id, c.Name, c.Color)).ToList();
    }
}
