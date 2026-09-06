import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameMode, GameState, Player } from './game.models';
import { GameService } from './game.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private readonly gameService = inject(GameService);

  game: GameState | null = null;
  selectedMode: GameMode = 'TwoPlayer';
  errorMessage = '';
  busy = false;

  readonly cells = Array.from({ length: 9 }, (_, index) => index);

  ngOnInit(): void {
    this.startNewGame();
  }

  startNewGame(): void {
    this.errorMessage = '';
    this.busy = true;

    this.gameService.createGame(this.selectedMode).subscribe({
      next: state => {
        this.game = state;
        this.busy = false;
      },
      error: err => this.handleError(err)
    });
  }

  onModeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedMode = select.value as GameMode;
    this.startNewGame();
  }

  play(cell: number): void {
    if (!this.game || this.busy)
      return;

    if (this.game.status !== 'InProgress')
      return;

    if (this.game.board[cell] !== null)
      return;

    this.busy = true;
    this.errorMessage = '';

    this.gameService
      .move(this.game.gameId, this.game.currentPlayer, cell)
      .subscribe({
        next: state => {
          this.game = state;
          this.busy = false;
        },
        error: err => this.handleError(err)
      });
  }

  undo(): void {
    if (!this.game || this.game.moveHistory.length === 0)
      return;

    if (this.game.status !== 'InProgress')
      return;

    this.busy = true;
    this.errorMessage = '';

    this.gameService.undo(this.game.gameId).subscribe({
      next: state => {
        this.game = state;
        this.busy = false;
      },
      error: err => this.handleError(err)
    });
  }

  resetGame(): void {
    if (!this.game)
      return;

    this.busy = true;
    this.errorMessage = '';

    this.gameService.resetGame(this.game.gameId).subscribe({
      next: state => {
        this.game = state;
        this.busy = false;
      },
      error: err => this.handleError(err)
    });
  }

  resetScoreboard(): void {
    this.busy = true;
    this.errorMessage = '';

    this.gameService.resetScoreboard().subscribe({
      next: score => {
        if (this.game) {
          this.game = {
            ...this.game,
            scoreboard: score
          };
        }
        this.busy = false;
      },
      error: err => this.handleError(err)
    });
  }

  cellLabel(cell: number): string {
    const row = Math.floor(cell / 3) + 1;
    const column = (cell % 3) + 1;
    return `Row ${row}, Column ${column}`;
  }

  isWinningCell(cell: number): boolean {
    return this.game?.winningCells.includes(cell) ?? false;
  }

  statusMessage(): string {
    if (!this.game)
      return 'Starting game...';

    if (this.game.status === 'Won')
      return `${this.game.winner} wins!`;

    if (this.game.status === 'Draw')
      return 'Game drawn!';

    if (this.game.mode === 'Computer' && this.game.currentPlayer === 'O')
      return 'Computer is thinking...';

    return `Player ${this.game.currentPlayer}'s turn`;
  }

  private handleError(err: any): void {
    this.busy = false;
    this.errorMessage =
      err?.error?.message ??
      'Unable to complete the operation. Please try again.';
  }
}
