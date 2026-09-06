# API Contract

## Create game

`POST /api/games`

Request:

```json
{
  "mode": "TwoPlayer"
}
```

Response contains the complete game state.

## Get game

`GET /api/games/{id}`

Returns the complete authoritative state.

## Make move

`POST /api/games/{id}/moves`

Request:

```json
{
  "player": "X",
  "cell": 0
}
```

The backend validates:

- cell is 0..8
- game is still in progress
- cell is empty
- submitted player matches current player
- Computer mode only accepts human X moves

In Computer mode, the same request also causes the backend to select and apply O's move if the game remains in progress.

## Undo

`POST /api/games/{id}/undo`

Two Player:
- removes one move.

Computer:
- removes O's latest move and the immediately preceding X move.

Undo is disabled after Won/Draw.

## Reset game

`POST /api/games/{id}/reset`

Clears board/history/status and sets X as current player. Scoreboard remains unchanged.

## Scoreboard

`GET /api/scoreboard`

Returns X wins, O wins and draws.

`POST /api/scoreboard/reset`

Clears all scoreboard counters.
