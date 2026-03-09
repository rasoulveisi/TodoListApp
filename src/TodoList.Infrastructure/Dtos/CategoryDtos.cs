namespace TodoList.Infrastructure.Dtos;

public record CreateCategoryRequest(string Name, string? Color);

public record UpdateCategoryRequest(string Name, string? Color);

public record CategoryResponse(int Id, string Name, string? Color);
