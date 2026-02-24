namespace TodoList.Domain.Entities;

public class TodoTask
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Note { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsImportant { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public TodoTask(string title, string? note = null, DateOnly? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = Guid.NewGuid();
        Title = title.Trim();
        Note = note?.Trim();
        DueDate = dueDate;
        IsCompleted = false;
        IsImportant = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        IsCompleted = true;
    }

    public void Uncomplete()
    {
        IsCompleted = false;
    }

    public void MarkImportant()
    {
        IsImportant = true;
    }

    public void UnmarkImportant()
    {
        IsImportant = false;
    }

    public void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
    }

    public void SetNote(string? note)
    {
        Note = note?.Trim();
    }

    public void SetDueDate(DateOnly? dueDate)
    {
        DueDate = dueDate;
    }
}
