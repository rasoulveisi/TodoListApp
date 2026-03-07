namespace TodoList.Domain.Entities;

public class TaskCategory
{
    public int TodoTaskId { get; set; }
    public TodoTask? TodoTask { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}