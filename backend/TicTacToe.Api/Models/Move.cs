namespace TicTacToe.Api.Models;

public sealed record Move(
    int MoveNumber,
    Player Player,
    int Cell);
