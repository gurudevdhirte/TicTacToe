using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.DTOs;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController : ControllerBase
{
    private readonly GameService _service;

    public ScoreboardController(GameService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<ScoreboardResponse> Get() =>
        Ok(_service.GetScoreboard());

    [HttpPost("reset")]
    public ActionResult<ScoreboardResponse> Reset() =>
        Ok(_service.ResetScoreboard());
}
