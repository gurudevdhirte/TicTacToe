using TicTacToe.Api.Models;

namespace TicTacToe.Api.DTOs;

public sealed record CreateGameRequest(GameMode Mode);

public sealed record MakeMoveRequest(Player Player, int Cell);

public sealed record MoveResponse(
    int MoveNumber,
    Player Player,
    int Cell);

public sealed record GameStateResponse(
    Guid GameId,
    Player?[] Board,
    Player CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveResponse> MoveHistory,
    Scoreboard Scoreboard);

public sealed record ScoreboardResponse(
    int XWins,
    int OWins,
    int Draws);
