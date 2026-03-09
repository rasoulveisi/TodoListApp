using MediatR;
using TodoList.Domain.Abstractions;
using TodoList.Infrastructure.Dtos;
using TodoList.Infrastructure.Exceptions;

namespace TodoList.Api.Features.TodoItems;

public record CreateTodoItemCommand(CreateTodoItemRequest Request) : IRequest<TodoItemResponse>;

public class CreateTodoItemCommandHandler(
    ITodoItemAbstraction todoItems,
    ITodoListAbstraction todoLists,
    ICategoryAbstraction categories)
    : IRequestHandler<CreateTodoItemCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Request.Title))
            throw new ValidationException(new Dictionary<string, string[]> { ["Title"] = ["Title is required."] });

        int? todoListId = request.Request.TodoListId;
        if (todoListId.HasValue && todoListId.Value != 0)
        {
            var list = await todoLists.GetByIdAsync(todoListId.Value, cancellationToken);
            if (list == null)
                throw new NotFoundException("TodoList", todoListId.Value);
        }
        else
            todoListId = null;

        var categoryEntities = new List<Domain.Entities.Category>();
        var categoryIds = request.Request.CategoryIds ?? [];
        if (categoryIds.Count != 0)
        {
            var allCategories = await categories.GetAllAsync(cancellationToken);
            var lookup = allCategories.ToDictionary(c => c.Id);
            foreach (var id in categoryIds)
            {
                if (lookup.TryGetValue(id, out var cat))
                    categoryEntities.Add(cat);
            }
        }

        var item = new Domain.Entities.TodoItem
        {
            Title = request.Request.Title,
            Description = request.Request.Description,
            TodoListId = todoListId,
            DueDate = request.Request.DueDate,
            IsImportant = request.Request.IsImportant,
            IsInMyDay = request.Request.IsInMyDay,
            CreatedAt = DateTime.UtcNow,
            Recurrence = request.Request.Recurrence is null ? null : new Domain.ValueObjects.RecurrencePattern
            {
                Type = request.Request.Recurrence.Type,
                Interval = request.Request.Recurrence.Interval,
                EndDate = request.Request.Recurrence.EndDate
            },
            Categories = categoryEntities
        };

        await todoItems.AddAsync(item, cancellationToken);
        return item.ToResponse();
    }
}
