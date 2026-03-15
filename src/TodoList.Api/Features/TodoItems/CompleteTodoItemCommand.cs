using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.TodoItems;

public record CompleteTodoItemCommand(int Id, bool Complete) : IRequest<TodoItemResponse>;

public class CompleteTodoItemCommandHandler(ITodoItemAbstraction todoItems)
    : IRequestHandler<CompleteTodoItemCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(CompleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var item = await todoItems.GetByIdAsync(request.Id, cancellationToken);
        if (item == null)
            throw new NotFoundException("TodoItem", request.Id);
        item.Complete();
        await todoItems.UpdateAsync(item, cancellationToken);
        return item.ToResponse();
    }
}
