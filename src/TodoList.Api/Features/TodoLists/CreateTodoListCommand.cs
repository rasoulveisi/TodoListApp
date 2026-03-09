using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoListEntity = TodoList.Domain.Entities.TodoList;

namespace TodoList.Api.Features.TodoLists;

public record CreateTodoListCommand(CreateTodoListRequest Request) : IRequest<TodoListResponse>;

public class CreateTodoListCommandHandler(ITodoListAbstraction todoLists)
    : IRequestHandler<CreateTodoListCommand, TodoListResponse>
{
    public async Task<TodoListResponse> Handle(CreateTodoListCommand request, CancellationToken cancellationToken)
    {
        var list = new TodoListEntity
        {
            Name = request.Request.Name,
            CreatedAt = DateTime.UtcNow
        };
        await todoLists.AddAsync(list, cancellationToken);
        return new TodoListResponse(list.Id, list.Name, list.CreatedAt, 0);
    }
}
