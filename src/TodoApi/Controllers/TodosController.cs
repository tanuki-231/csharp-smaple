using Microsoft.AspNetCore.Mvc;
using TodoApi.Application;
using TodoApi.Domain.Exceptions;
using TodoApi.Models.Requests;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosController(ITodoService todoService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTodos(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var todos = await todoService.ListAsync(userId, cancellationToken);
        return Ok(todos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodo([FromBody] TodoUpsertRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var created = await todoService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/todos/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTodo(Guid id, [FromBody] TodoUpsertRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var updated = await todoService.UpdateAsync(userId, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTodo(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        await todoService.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    private string RequireUserId()
        => HttpContext.Items["UserId"]?.ToString() ?? throw new UnauthorizedException();
}
