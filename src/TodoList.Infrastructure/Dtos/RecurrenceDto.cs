using TodoList.Domain.Enums;

namespace TodoList.Infrastructure.Dtos;

public record RecurrenceDto(RecurrenceType Type, int Interval, DateTime? EndDate);
