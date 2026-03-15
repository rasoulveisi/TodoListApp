using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.Categories;

public record UpdateCategoryCommand(int Id, UpdateCategoryRequest Request) : IRequest<CategoryResponse>;

public class UpdateCategoryCommandHandler(ICategoryAbstraction categories)
    : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            throw new NotFoundException("Category", request.Id);
        category.Name = request.Request.Name;
        category.Color = request.Request.Color;
        await categories.UpdateAsync(category, cancellationToken);
        return new CategoryResponse(category.Id, category.Name, category.Color);
    }
}
