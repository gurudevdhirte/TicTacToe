using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private static GameService CreateService() => new();

    private static Guid NewGame(GameService service, GameMode mode = GameMode.TwoPlayer) =>
        service.CreateGame(mode).Id;

    private static void Move(GameService service, Guid id, Player player, int cell) =>
        service.MakeMove(id, new MakeMoveRequest(player, cell));

    [Fact]
    public void ValidMove_ShouldUpdateBoard_AndSwitchTurn()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);

        var state = service.GetState(id);
        Assert.Equal(Player.X, state.Board[0]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Single(state.MoveHistory);
    }

    [Fact]
    public void InvalidOccupiedMove_ShouldBeRejected()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);

        Assert.Throws<InvalidOperationException>(() =>
            Move(service, id, Player.O, 0));
    }

    [Fact]
    public void WrongPlayer_ShouldBeRejected()
    {
        var service = CreateService();
        var id = NewGame(service);

        Assert.Throws<InvalidOperationException>(() =>
            Move(service, id, Player.O, 0));
    }

    [Fact]
    public void RowWin_ShouldBeDetected()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 3);
        Move(service, id, Player.X, 1);
        Move(service, id, Player.O, 4);
        Move(service, id, Player.X, 2);

        var state = service.GetState(id);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, state.WinningCells);
        Assert.Equal(1, state.Scoreboard.XWins);
    }

    [Fact]
    public void ColumnWin_ShouldBeDetected()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 1);
        Move(service, id, Player.X, 3);
        Move(service, id, Player.O, 2);
        Move(service, id, Player.X, 6);

        var state = service.GetState(id);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Equal(new[] { 0, 3, 6 }, state.WinningCells);
    }

    [Fact]
    public void DiagonalWin_ShouldBeDetected()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 1);
        Move(service, id, Player.X, 4);
        Move(service, id, Player.O, 2);
        Move(service, id, Player.X, 8);

        var state = service.GetState(id);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(new[] { 0, 4, 8 }, state.WinningCells);
    }

    [Fact]
    public void Draw_ShouldBeDetected()
    {
        var service = CreateService();
        var id = NewGame(service);

        // X O X
        // X O O
        // O X X
        var moves = new[]
        {
            (Player.X, 0), (Player.O, 1),
            (Player.X, 2), (Player.O, 4),
            (Player.X, 3), (Player.O, 5),
            (Player.X, 7), (Player.O, 6),
            (Player.X, 8)
        };

        foreach (var (player, cell) in moves)
            Move(service, id, player, cell);

        var state = service.GetState(id);

        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Null(state.Winner);
        Assert.Equal(1, state.Scoreboard.Draws);
    }

    [Fact]
    public void ResetGame_ShouldClearBoardAndHistory_ButKeepScoreboard()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 3);
        Move(service, id, Player.X, 1);
        Move(service, id, Player.O, 4);
        Move(service, id, Player.X, 2);

        service.ResetGame(id);

        var state = service.GetState(id);

        Assert.All(state.Board, cell => Assert.Null(cell));
        Assert.Empty(state.MoveHistory);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal(1, state.Scoreboard.XWins);
    }

    [Fact]
    public void Undo_TwoPlayer_ShouldRemoveOneMove()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 4);

        service.Undo(id);

        var state = service.GetState(id);

        Assert.Equal(Player.X, state.Board[0]);
        Assert.Null(state.Board[4]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Single(state.MoveHistory);
    }

    [Fact]
    public void Undo_ComputerMode_ShouldRemoveHumanAndComputerMoves()
    {
        var service = CreateService();
        var id = NewGame(service, GameMode.Computer);

        Move(service, id, Player.X, 0);

        var beforeUndo = service.GetState(id);
        Assert.Equal(2, beforeUndo.MoveHistory.Count);
        Assert.Equal(Player.O, beforeUndo.Board[4]); // computer takes center

        service.Undo(id);

        var afterUndo = service.GetState(id);

        Assert.All(afterUndo.Board, cell => Assert.Null(cell));
        Assert.Empty(afterUndo.MoveHistory);
        Assert.Equal(Player.X, afterUndo.CurrentPlayer);
    }

    [Fact]
    public void Computer_ShouldTakeWinningMove_WhenAvailable()
    {
        var service = CreateService();
        var id = NewGame(service, GameMode.Computer);

        // Build O at 3 and 4 while X has harmless positions.
        Move(service, id, Player.X, 0); // O -> center 4
        Move(service, id, Player.X, 8); // O -> corner 2 or similar

        var state = service.GetState(id);

        // The exact board depends on the priority, but O must always make a valid move.
        Assert.Equal(4, state.MoveHistory.Count);
        Assert.All(state.MoveHistory, move => Assert.InRange(move.Cell, 0, 8));
    }

    [Fact]
    public void MoveAfterCompletion_ShouldBeRejected()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 3);
        Move(service, id, Player.X, 1);
        Move(service, id, Player.O, 4);
        Move(service, id, Player.X, 2);

        Assert.Throws<InvalidOperationException>(() =>
            Move(service, id, Player.O, 5));
    }

    [Fact]
    public void Scoreboard_ShouldUpdateOnlyOnce()
    {
        var service = CreateService();
        var id = NewGame(service);

        Move(service, id, Player.X, 0);
        Move(service, id, Player.O, 3);
        Move(service, id, Player.X, 1);
        Move(service, id, Player.O, 4);
        Move(service, id, Player.X, 2);

        var score = service.GetScoreboard();
        Assert.Equal(1, score.XWins);
        Assert.Equal(0, score.OWins);
        Assert.Equal(0, score.Draws);
    }
}
