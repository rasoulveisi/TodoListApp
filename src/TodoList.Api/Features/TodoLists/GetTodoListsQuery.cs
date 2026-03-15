using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.TodoLists;

public record GetTodoListsQuery : IRequest<List<TodoListResponse>>;

public class GetTodoListsQueryHandler(ITodoListAbstraction todoLists)
    : IRequestHandler<GetTodoListsQuery, List<TodoListResponse>>
{
    public async Task<List<TodoListResponse>> Handle(GetTodoListsQuery request, CancellationToken cancellationToken)
    {
        var lists = await todoLists.GetAllAsync(cancellationToken);
        return lists.Select(l => new TodoListResponse(l.Id, l.Name, l.CreatedAt, l.Items.Count)).ToList();
    }
}
