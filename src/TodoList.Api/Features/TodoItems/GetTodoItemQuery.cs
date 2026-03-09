using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.TodoItems;

public record GetTodoItemQuery(int Id) : IRequest<TodoItemResponse?>;

public class GetTodoItemQueryHandler(ITodoItemAbstraction todoItems)
    : IRequestHandler<GetTodoItemQuery, TodoItemResponse?>
{
    public async Task<TodoItemResponse?> Handle(GetTodoItemQuery request, CancellationToken cancellationToken)
    {
        var item = await todoItems.GetByIdAsync(request.Id, cancellationToken);
        return item?.ToResponse();
    }
}
