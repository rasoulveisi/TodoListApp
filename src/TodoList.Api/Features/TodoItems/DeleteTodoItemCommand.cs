using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.TodoItems;

public record DeleteTodoItemCommand(int Id) : IRequest<Unit>;

public class DeleteTodoItemCommandHandler(ITodoItemAbstraction todoItems)
    : IRequestHandler<DeleteTodoItemCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var item = await todoItems.GetByIdAsync(request.Id, cancellationToken);
        if (item == null)
            throw new NotFoundException("TodoItem", request.Id);
        await todoItems.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
