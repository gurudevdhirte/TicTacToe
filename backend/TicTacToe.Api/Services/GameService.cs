using System.Collections.Concurrent;
using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public sealed class GameService
{
    private readonly ConcurrentDictionary<Guid, GameSession> _games = new();
    private readonly object _scoreboardLock = new();
    private readonly Scoreboard _scoreboard = new();

    private static readonly int[][] WinningLines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6]
    ];

    public GameSession CreateGame(GameMode mode)
    {
        var game = new GameSession { Mode = mode };
        _games[game.Id] = game;
        return game;
    }

    public GameSession GetGame(Guid id) =>
        _games.TryGetValue(id, out var game)
            ? game
            : throw new KeyNotFoundException("Game not found.");

    public GameStateResponse GetState(Guid id)
    {
        var game = GetGame(id);
        lock (game.SyncRoot)
        {
            return ToResponse(game);
        }
    }

    public GameStateResponse MakeMove(Guid id, MakeMoveRequest request)
    {
        var game = GetGame(id);

        lock (game.SyncRoot)
        {
            ValidateMove(game, request.Player, request.Cell);
            ApplyMove(game, request.Player, request.Cell);

            if (game.Status == GameStatus.InProgress &&
                game.Mode == GameMode.Computer &&
                request.Player == Player.X)
            {
                var computerCell = ChooseComputerMove(game);
                ApplyMove(game, Player.O, computerCell);
            }

            return ToResponse(game);
        }
    }

    public GameStateResponse Undo(Guid id)
    {
        var game = GetGame(id);

        lock (game.SyncRoot)
        {
            if (game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Undo is disabled after game completion.");

            if (game.Moves.Count == 0)
                throw new InvalidOperationException("There are no moves to undo.");

            if (game.Mode == GameMode.TwoPlayer)
            {
                RemoveLastMove(game);
            }
            else
            {
                // In computer mode, remove the computer's O move and
                // the preceding human X move as one logical action.
                RemoveLastMove(game);

                if (game.Moves.Count > 0 && game.Moves[^1].Player == Player.X)
                    RemoveLastMove(game);
            }

            RecalculateState(game);
            return ToResponse(game);
        }
    }

    public GameStateResponse ResetGame(Guid id)
    {
        var oldGame = GetGame(id);

        lock (oldGame.SyncRoot)
        {
            Array.Clear(oldGame.Board, 0, oldGame.Board.Length);
            oldGame.Moves.Clear();
            oldGame.CurrentPlayer = Player.X;
            oldGame.Status = GameStatus.InProgress;
            oldGame.Winner = null;
            oldGame.WinningCells.Clear();
            oldGame.ScoreboardUpdated = false;

            return ToResponse(oldGame);
        }
    }

    public ScoreboardResponse GetScoreboard()
    {
        lock (_scoreboardLock)
        {
            return ToScoreboardResponse();
        }
    }

    public ScoreboardResponse ResetScoreboard()
    {
        lock (_scoreboardLock)
        {
            _scoreboard.XWins = 0;
            _scoreboard.OWins = 0;
            _scoreboard.Draws = 0;
            return ToScoreboardResponse();
        }
    }

    private void ValidateMove(GameSession game, Player player, int cell)
    {
        if (cell < 0 || cell >= 9)
            throw new ArgumentOutOfRangeException(nameof(cell), "Cell must be between 0 and 8.");

        if (game.Status != GameStatus.InProgress)
            throw new InvalidOperationException("The game is already completed.");

        if (game.Board[cell] is not null)
            throw new InvalidOperationException("The selected cell is already occupied.");

        if (player != game.CurrentPlayer)
            throw new InvalidOperationException($"It is {game.CurrentPlayer}'s turn.");

        if (game.Mode == GameMode.Computer && player != Player.X)
            throw new InvalidOperationException("Only X can make moves in Computer mode.");
    }

    private void ApplyMove(GameSession game, Player player, int cell)
    {
        game.Board[cell] = player;

        game.Moves.Add(new Move(
            game.Moves.Count + 1,
            player,
            cell));

        EvaluateAfterMove(game, player);
    }

    private void EvaluateAfterMove(GameSession game, Player player)
    {
        var winningLine = WinningLines.FirstOrDefault(line =>
            line.All(cell => game.Board[cell] == player));

        if (winningLine is not null)
        {
            game.Status = GameStatus.Won;
            game.Winner = player;
            game.WinningCells.Clear();
            game.WinningCells.AddRange(winningLine);
            game.CurrentPlayer = player;
            UpdateScoreboardOnce(game);
            return;
        }

        if (game.Board.All(cell => cell is not null))
        {
            game.Status = GameStatus.Draw;
            game.Winner = null;
            game.WinningCells.Clear();
            UpdateScoreboardOnce(game);
            return;
        }

        game.CurrentPlayer = Opponent(player);
    }

    private void RecalculateState(GameSession game)
    {
        game.Status = GameStatus.InProgress;
        game.Winner = null;
        game.WinningCells.Clear();

        if (game.Moves.Count == 0)
        {
            game.CurrentPlayer = Player.X;
            return;
        }

        var lastPlayer = game.Moves[^1].Player;
        game.CurrentPlayer = Opponent(lastPlayer);
    }

    private static void RemoveLastMove(GameSession game)
    {
        var move = game.Moves[^1];
        game.Board[move.Cell] = null;
        game.Moves.RemoveAt(game.Moves.Count - 1);
    }

    private void UpdateScoreboardOnce(GameSession game)
    {
        if (game.ScoreboardUpdated)
            return;

        lock (_scoreboardLock)
        {
            if (game.ScoreboardUpdated)
                return;

            if (game.Status == GameStatus.Won)
            {
                if (game.Winner == Player.X)
                    _scoreboard.XWins++;
                else
                    _scoreboard.OWins++;
            }
            else if (game.Status == GameStatus.Draw)
            {
                _scoreboard.Draws++;
            }

            game.ScoreboardUpdated = true;
        }
    }

    private static int ChooseComputerMove(GameSession game)
    {
        // 1. O can win.
        var winningMove = FindWinningMove(game, Player.O);
        if (winningMove.HasValue)
            return winningMove.Value;

        // 2. X can win next -> block.
        var blockingMove = FindWinningMove(game, Player.X);
        if (blockingMove.HasValue)
            return blockingMove.Value;

        // 3. Center.
        if (game.Board[4] is null)
            return 4;

        // 4. Corner.
        foreach (var cell in new[] { 0, 2, 6, 8 })
        {
            if (game.Board[cell] is null)
                return cell;
        }

        // 5. Any available cell.
        return Enumerable.Range(0, 9)
            .First(cell => game.Board[cell] is null);
    }

    private static int? FindWinningMove(GameSession game, Player player)
    {
        foreach (var cell in Enumerable.Range(0, 9))
        {
            if (game.Board[cell] is not null)
                continue;

            game.Board[cell] = player;
            var wins = WinningLines.Any(line =>
                line.All(index => game.Board[index] == player));
            game.Board[cell] = null;

            if (wins)
                return cell;
        }

        return null;
    }

    private static Player Opponent(Player player) =>
        player == Player.X ? Player.O : Player.X;

    private GameStateResponse ToResponse(GameSession game)
    {
        ScoreboardResponse score;

        lock (_scoreboardLock)
        {
            score = ToScoreboardResponse();
        }

        return new GameStateResponse(
            game.Id,
            game.Board.ToArray(),
            game.CurrentPlayer,
            game.Mode,
            game.Status,
            game.Winner,
            game.WinningCells.ToArray(),
            game.Moves
                .Select(m => new MoveResponse(m.MoveNumber, m.Player, m.Cell))
                .ToArray(),
            new Scoreboard
            {
                XWins = score.XWins,
                OWins = score.OWins,
                Draws = score.Draws
            });
    }

    private ScoreboardResponse ToScoreboardResponse() =>
        new(_scoreboard.XWins, _scoreboard.OWins, _scoreboard.Draws);
}
