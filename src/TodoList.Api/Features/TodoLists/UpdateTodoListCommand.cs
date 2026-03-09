using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;
using TodoListEntity = TodoList.Domain.Entities.TodoList;

namespace TodoList.Api.Features.TodoLists;

public record UpdateTodoListCommand(int Id, UpdateTodoListRequest Request) : IRequest<TodoListResponse>;

public class UpdateTodoListCommandHandler(ITodoListAbstraction todoLists)
    : IRequestHandler<UpdateTodoListCommand, TodoListResponse>
{
    public async Task<TodoListResponse> Handle(UpdateTodoListCommand request, CancellationToken cancellationToken)
    {
        var list = await todoLists.GetByIdAsync(request.Id, cancellationToken);
        if (list == null)
            throw new NotFoundException("TodoList", request.Id);
        list.Name = request.Request.Name;
        await todoLists.UpdateAsync(list, cancellationToken);
        return new TodoListResponse(list.Id, list.Name, list.CreatedAt, list.Items.Count);
    }
}
