using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.TodoLists;

public record DeleteTodoListCommand(int Id) : IRequest<Unit>;

public class DeleteTodoListCommandHandler(ITodoListAbstraction todoLists)
    : IRequestHandler<DeleteTodoListCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken)
    {
        var list = await todoLists.GetByIdAsync(request.Id, cancellationToken);
        if (list == null)
            throw new NotFoundException("TodoList", request.Id);
        await todoLists.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
