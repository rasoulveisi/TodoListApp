namespace TodoList.Infrastructure.Dtos;

public record CreateTodoItemRequest(
    string Title,
    string? Description,
    int? TodoListId,
    DateTime? DueDate,
    bool IsImportant,
    bool IsInMyDay,
    RecurrenceDto? Recurrence,
    IReadOnlyList<int> CategoryIds);

public record UpdateTodoItemRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    bool IsImportant,
    bool IsInMyDay,
    bool IsCompleted,
    RecurrenceDto? Recurrence,
    IReadOnlyList<int> CategoryIds);

public record TodoItemResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime? DueDate,
    bool IsImportant,
    bool IsInMyDay,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    RecurrenceDto? Recurrence,
    int? TodoListId,
    IReadOnlyList<CategorySummaryDto> Categories);

public record CategorySummaryDto(int Id, string Name, string? Color);
