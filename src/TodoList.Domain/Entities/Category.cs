namespace TodoList.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<TodoTask>? Tasks { get; set; }
    public ICollection<TaskCategory>? TaskCategories { get; set; }
}