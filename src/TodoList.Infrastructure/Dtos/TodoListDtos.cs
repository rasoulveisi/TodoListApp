namespace TodoList.Infrastructure.Dtos;

public record CreateTodoListRequest(string Name);

public record UpdateTodoListRequest(string Name);

public record TodoListResponse(int Id, string Name, DateTime CreatedAt, int ItemCount);

public record TodoListWithItemsResponse(
    int Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<TodoItemSummaryDto> Items);

public record TodoItemSummaryDto(
    int Id,
    string Title,
    bool IsCompleted,
    DateTime? DueDate,
    bool IsImportant);
