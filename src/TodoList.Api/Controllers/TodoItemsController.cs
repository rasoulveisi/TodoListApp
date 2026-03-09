using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoList.Api.Features.TodoItems;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] bool? myDay,
        [FromQuery] bool? planned,
        [FromQuery] bool? important,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetTodoItemsQuery(IsInMyDay: myDay, HasDueDate: planned, IsImportant: important, Page: page, PageSize: pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await mediator.Send(new GetTodoItemQuery(id), cancellationToken);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoItemRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CreateTodoItemCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTodoItemRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UpdateTodoItemCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteTodoItemCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteRequest body, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CompleteTodoItemCommand(id, body.Complete), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/myday")]
    public async Task<IActionResult> ToggleMyDay(int id, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ToggleMyDayCommand(id), cancellationToken);
        return Ok(result);
    }
}

public record CompleteRequest(bool Complete);
