using TodoList.Domain.ValueObjects;

namespace TodoList.Domain.Entities;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsImportant { get; set; }
    public bool IsInMyDay { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public RecurrencePattern? Recurrence { get; set; }

    public int TodoListId { get; set; }
    public TodoList TodoList { get; set; } = null!;

    public List<Category> Categories { get; set; } = [];
}
