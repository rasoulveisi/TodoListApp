using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.TodoItems;

public record ToggleMyDayCommand(int Id) : IRequest<TodoItemResponse>;

public class ToggleMyDayCommandHandler(ITodoItemAbstraction todoItems)
    : IRequestHandler<ToggleMyDayCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(ToggleMyDayCommand request, CancellationToken cancellationToken)
    {
        var item = await todoItems.GetByIdAsync(request.Id, cancellationToken);
        if (item == null)
            throw new NotFoundException("TodoItem", request.Id);
        if (item.IsInMyDay)
            item.UnmarkAsMyDay();
        else
            item.MarkAsMyDay();
        await todoItems.UpdateAsync(item, cancellationToken);
        return item.ToResponse();
    }
}
