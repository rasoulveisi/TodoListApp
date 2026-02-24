using TodoList.Domain.Enums;

namespace TodoList.Domain.Entities;

public class TodoTask
{
    public int Id { get; set; }
    public string Title { get;  set; }
    public string? Note { get;  set; }
    public DateOnly? DueDate { get;  set; }
    public bool IsCompleted { get;  set; }
    public bool IsImportant { get;  set; }
    public DateTime CreatedAt { get;  set; }
    public TaskTypeEnum TaskType { get;  set; }
}
