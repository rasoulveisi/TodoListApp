using TodoList.Domain.Enums;

namespace TodoList.Domain.ValueObjects;

public record RecurrencePattern
{
    public RecurrenceType Type { get; set; }
    public int Interval { get; set; }
    public DateTime? EndDate { get; set; }
}
