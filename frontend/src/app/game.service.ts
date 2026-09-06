import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { GameMode, GameState, Player, Scoreboard } from './game.models';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  createGame(mode: GameMode): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games`, { mode });
  }

  getGame(id: string): Observable<GameState> {
    return this.http.get<GameState>(`${this.baseUrl}/games/${id}`);
  }

  move(id: string, player: Player, cell: number): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${id}/moves`, {
      player,
      cell
    });
  }

  undo(id: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${id}/undo`, {});
  }

  resetGame(id: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${id}/reset`, {});
  }

  getScoreboard(): Observable<Scoreboard> {
    return this.http.get<Scoreboard>(`${this.baseUrl}/scoreboard`);
  }

  resetScoreboard(): Observable<Scoreboard> {
    return this.http.post<Scoreboard>(`${this.baseUrl}/scoreboard/reset`, {});
  }
}
