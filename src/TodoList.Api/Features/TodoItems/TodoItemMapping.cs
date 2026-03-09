using TodoList.Domain.Entities;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Features.TodoItems;

internal static class TodoItemMapping
{
    public static TodoItemResponse ToResponse(this TodoItem item) =>
        new(
            item.Id,
            item.Title,
            item.Description,
            item.IsCompleted,
            item.DueDate,
            item.IsImportant,
            item.IsInMyDay,
            item.CreatedAt,
            item.CompletedAt,
            item.Recurrence is null ? null : new RecurrenceDto(item.Recurrence.Type, item.Recurrence.Interval, item.Recurrence.EndDate),
            item.TodoListId,
            item.Categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Color)).ToList()
        );

    public static TodoItemSummaryDto ToSummaryDto(this TodoItem item) =>
        new(item.Id, item.Title, item.IsCompleted, item.DueDate, item.IsImportant);
}
