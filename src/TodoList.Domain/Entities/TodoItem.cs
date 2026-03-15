using TodoList.Domain.ValueObjects;

namespace TodoList.Domain.Entities;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; private set; } = false;
    public DateTime? DueDate { get; set; }
    public bool IsImportant { get; private set; } = false;
    public bool IsInMyDay { get; private set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public RecurrencePattern? Recurrence { get; set; }

    public int? TodoListId { get; set; }
    public TodoList? TodoList { get; set; }

    public List<Category> Categories { get; set; } = [];

    /// <summary>
    /// Used by Entity Framework Core when materializing entities from the database.
    /// Owned types (e.g. Recurrence) cannot be bound to constructor parameters.
    /// </summary>
    private TodoItem() { }

    public TodoItem(string title, string? description, DateTime? dueDate, bool isImportant, bool isInMyDay, RecurrencePattern? recurrence)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        if (dueDate.HasValue && dueDate.Value.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Due date must be today or in the future", nameof(dueDate));
        if (recurrence is not null && recurrence.EndDate.HasValue && recurrence.EndDate.Value.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Recurrence end date must be today or in the future", nameof(recurrence));
        
        Title = title;
        Description = description;
        DueDate = dueDate;
        IsImportant = isImportant;
        IsInMyDay = isInMyDay;
        Recurrence = recurrence;
    }

    public void Complete()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }

    public void Uncomplete()
    {
        IsCompleted = false;
        CompletedAt = null;
    }

    public void MarkAsMyDay()
    {
        IsInMyDay = true;
    }
    public void UnmarkAsMyDay()
    {
        IsInMyDay = false;
    }

    public void MarkAsImportant()
    {
        IsImportant = true;
    }
    public void UnmarkAsImportant()
    {
        IsImportant = false;
    }
}
