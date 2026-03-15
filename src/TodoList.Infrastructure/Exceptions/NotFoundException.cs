namespace TodoList.Infrastructure.Exceptions;

public class NotFoundException : Exception
{
    public string ResourceName { get; }
    public object ResourceId { get; }

    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with id '{resourceId}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    public NotFoundException(string message) : base(message)
    {
        ResourceName = string.Empty;
        ResourceId = null!;
    }
}
