namespace TodoList.Domain.Entities;

public class TodoList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public List<TodoItem> Items { get; set; } = [];
}
