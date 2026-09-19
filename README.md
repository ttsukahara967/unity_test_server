# unity_test_server

API that receives scores from the memory game ([unity_test](https://github.com/ttsukahara967/unity_test)). ASP.NET Core (.NET 10) + MySQL 8.4, JWT authentication, run with Docker Compose.

## Related repositories

| Repository | Description |
|---|---|
| [unity_test](https://github.com/ttsukahara967/unity_test) | The memory (concentration) card game made with Unity. It logs in to this API, submits its score when a round is cleared, and shows the ranking |

## Run

```bash
docker compose up -d --build
```

- API: `http://localhost:5080` (this machine only; change the port with `API_PORT`)
- Development login: `id = user1` / `password = pass` (created automatically on startup)
- Stop: `docker compose down` (add `-v` to delete the database too)
- Inspect MySQL directly: `docker compose exec db mysql -uapp -papppass unity_test`

To change settings, copy `.env.example` to `.env` and edit it.

## API docs (Scalar)

After starting, open <http://localhost:5080/scalar/v1> to browse the API and try it out in place (the OpenAPI definition is at `/openapi/v1.json`).

1. Run `POST /api/login` and copy the `token`.
2. Paste it into **Authentication → Bearer Token** at the top of the page.
3. You can now try the endpoints with a lock icon (submit score, my scores, ranking).

The docs are served only in the `Development` environment. `docker-compose.yml` defaults to `Development`; set `ASPNETCORE_ENVIRONMENT=Production` in `.env` to disable them.

## API

Everything except `/api/login` and `/health` requires `Authorization: Bearer <token>`. JSON keys are camelCase.

| Method | Path | Description |
|---|---|---|
| POST | `/api/login` | `{ "id", "password" }` → `{ "token", "expiresAt" }` (valid for 60 minutes) |
| POST | `/api/scores` | Saves `{ "moves", "pairs", "elapsedSeconds"? }`. Returns the saved score with 201 |
| GET | `/api/scores/me?limit=20` | Your own scores, newest first |
| GET | `/api/scores/ranking?pairs=8&limit=10` | Ranking per number of pairs (each user's fewest moves; fewer is better) |
| GET | `/health` | Liveness check |

The request is rejected with 400 unless `moves` is at least `pairs` and `pairs` is between 1 and 32.

```bash
TOKEN=$(curl -s -X POST localhost:5080/api/login \
  -H 'Content-Type: application/json' \
  -d '{"id":"user1","password":"pass"}' | python3 -c 'import sys,json; print(json.load(sys.stdin)["token"])')

curl -s -X POST localhost:5080/api/scores \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"moves":12,"pairs":8,"elapsedSeconds":48.5}'

curl -s localhost:5080/api/scores/ranking -H "Authorization: Bearer $TOKEN"
```

## Notes for integrating the Unity game
- The server currently uses plain HTTP (not HTTPS), so set `Project Settings > Player > Other Settings > Allow downloads over HTTP` to **Always allowed** (UnityWebRequest rejects it by default).
- The game only needs to send `moves` (the `moves` field in `MemoryGame.cs`) and `pairs`. There is no timer yet, so `elapsedSeconds` can be omitted.

## Layout
```
docker-compose.yml         db (mysql:8.4) and api
src/ScoreApi/              ASP.NET Core minimal API
  Auth/                    JWT issuing and password hashing (PBKDF2-SHA256)
  Data/                    MySQL access (MySqlConnector + Dapper), table creation, development user seeding
  Endpoints/               /api/login, /api/scores
  OpenApi/                 API docs setup (OpenAPI + Scalar)
```
The tables (`users`, `scores`) are created on API startup with `CREATE TABLE IF NOT EXISTS`.

## Development settings (review before production)
- `JWT_KEY`, the database passwords, and `user1/pass` are development defaults.
- Not included: HTTPS, login rate limiting, and protection against tampered scores (the values reported by the client are stored as is).
