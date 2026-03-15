using MediatR;
using TodoList.Domain;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.TodoItems;

public record GetTodoItemsQuery(bool? IsInMyDay, bool? HasDueDate, bool? IsImportant, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<TodoItemResponse>>;

public class GetTodoItemsQueryHandler(ITodoItemAbstraction todoItems)
    : IRequestHandler<GetTodoItemsQuery, PaginatedResult<TodoItemResponse>>
{
    public async Task<PaginatedResult<TodoItemResponse>> Handle(GetTodoItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await todoItems.GetFilteredAsync(
            request.IsInMyDay,
            request.HasDueDate,
            request.IsImportant,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PaginatedResult<TodoItemResponse>(
            result.Items.Select(i => i.ToResponse()).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
