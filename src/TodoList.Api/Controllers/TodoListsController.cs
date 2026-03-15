using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoList.Api.Features.TodoLists;
using TodoList.Infrastructure.Dtos;

namespace TodoList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoListsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var lists = await mediator.Send(new GetTodoListsQuery(), cancellationToken);
        return Ok(lists);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var list = await mediator.Send(new GetTodoListQuery(id), cancellationToken);
        if (list is null) return NotFound();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoListRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CreateTodoListCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTodoListRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UpdateTodoListCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteTodoListCommand(id), cancellationToken);
        return NoContent();
    }
}
