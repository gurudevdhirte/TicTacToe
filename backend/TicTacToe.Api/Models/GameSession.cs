namespace TicTacToe.Api.Models;

public sealed class GameSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public GameMode Mode { get; init; }
    public Player?[] Board { get; } = new Player?[9];
    public List<Move> Moves { get; } = [];
    public Player CurrentPlayer { get; set; } = Player.X;
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public Player? Winner { get; set; }
    public List<int> WinningCells { get; } = [];
    public object SyncRoot { get; } = new();
    public bool ScoreboardUpdated { get; set; }
}
