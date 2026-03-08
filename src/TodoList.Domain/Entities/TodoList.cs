namespace TodoList.Domain.Entities;

public class TodoList
{
    public int Id {get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<TodoTask>? Tasks { get; set; }
}