using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Domain.Entities;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.Categories;

public record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<CategoryResponse>;

public class CreateCategoryCommandHandler(ICategoryAbstraction categories)
    : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Request.Name,
            Color = request.Request.Color
        };
        await categories.AddAsync(category, cancellationToken);
        return new CategoryResponse(category.Id, category.Name, category.Color);
    }
}
