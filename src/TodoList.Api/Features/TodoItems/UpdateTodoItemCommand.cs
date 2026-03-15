using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Domain.Entities;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;
using TodoList.Domain.ValueObjects;

namespace TodoList.Api.Features.TodoItems;

public record UpdateTodoItemCommand(int Id, UpdateTodoItemRequest Request) : IRequest<TodoItemResponse>;

public class UpdateTodoItemCommandHandler(
    ITodoItemAbstraction todoItems,
    ICategoryAbstraction categories)
    : IRequestHandler<UpdateTodoItemCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var item = await todoItems.GetByIdAsync(request.Id, cancellationToken);
        if (item == null)
            throw new NotFoundException("TodoItem", request.Id);

        item.Title = request.Request.Title;
        item.Description = request.Request.Description;
        item.DueDate = request.Request.DueDate;
        item.IsImportant = request.Request.IsImportant;
        item.IsInMyDay = request.Request.IsInMyDay;
        item.Recurrence = request.Request.Recurrence is null ? null : new RecurrencePattern
        {
            Type = request.Request.Recurrence.Type,
            Interval = request.Request.Recurrence.Interval,
            EndDate = request.Request.Recurrence.EndDate
        };

        var categoryIds = request.Request.CategoryIds ?? [];
        if (categoryIds.Count != 0)
        {
            var all = await categories.GetAllAsync(cancellationToken);
            var lookup = all.ToDictionary(c => c.Id);
            item.Categories = categoryIds.Where(id => lookup.ContainsKey(id)).Select(id => lookup[id]).ToList();
        }
        else
            item.Categories = [];

        await todoItems.UpdateAsync(item, cancellationToken);
        return item.ToResponse();
    }
}
