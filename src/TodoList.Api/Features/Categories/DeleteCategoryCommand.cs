using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.Categories;

public record DeleteCategoryCommand(int Id) : IRequest<Unit>;

public class DeleteCategoryCommandHandler(ICategoryAbstraction categories)
    : IRequestHandler<DeleteCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            throw new NotFoundException("Category", request.Id);
        await categories.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
