using TodoList.Domain.Enums;

namespace TodoList.Domain.Entities;

public class RepeatedTask: TodoTask
{
     public RepeatedTaskTypeEnum RepeatedTaskType { get; set; }
     public DateOnly? StartDate { get; set; }
     public DateOnly? EndDate { get; set; }
     public int? RepeatInterval { get; set; }

}