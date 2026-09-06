using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private readonly GameService _service;

    public GamesController(GameService service)
    {
        _service = service;
    }

    [HttpPost]
    public ActionResult<GameStateResponse> Create(CreateGameRequest request)
    {
        return Ok(_service.GetState(_service.CreateGame(request.Mode).Id));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameStateResponse> Get(Guid id)
    {
        try
        {
            return Ok(_service.GetState(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameStateResponse> Move(Guid id, MakeMoveRequest request)
    {
        try
        {
            return Ok(_service.MakeMove(id, request));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameStateResponse> Undo(Guid id)
    {
        try
        {
            return Ok(_service.Undo(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameStateResponse> Reset(Guid id)
    {
        try
        {
            return Ok(_service.ResetGame(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
