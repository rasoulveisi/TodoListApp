using MediatR;
using TodoList.Api.Features.TodoItems;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.TodoLists;

public record GetTodoListQuery(int Id) : IRequest<TodoListWithItemsResponse?>;

public class GetTodoListQueryHandler(ITodoListAbstraction todoLists)
    : IRequestHandler<GetTodoListQuery, TodoListWithItemsResponse?>
{
    public async Task<TodoListWithItemsResponse?> Handle(GetTodoListQuery request, CancellationToken cancellationToken)
    {
        var list = await todoLists.GetByIdAsync(request.Id, cancellationToken);
        if (list == null) return null;
        var items = list.Items.Select(i => i.ToSummaryDto()).ToList();
        return new TodoListWithItemsResponse(list.Id, list.Name, list.CreatedAt, items);
    }
}
