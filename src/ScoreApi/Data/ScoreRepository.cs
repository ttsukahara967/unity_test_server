using Dapper;
using MySqlConnector;

namespace ScoreApi.Data;

public sealed record ScoreRecord(long Id, int Moves, int Pairs, double? ElapsedSeconds, DateTime CreatedAt);

public sealed record RankingRow(string LoginId, int Moves);

public sealed class ScoreRepository(MySqlDataSource dataSource)
{
    const string SelectColumns =
        "id AS Id, moves AS Moves, pairs AS Pairs, elapsed_seconds AS ElapsedSeconds, created_at AS CreatedAt";

    public async Task<ScoreRecord> AddAsync(
        long userId, int moves, int pairs, double? elapsedSeconds, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await connection.QuerySingleAsync<ScoreRecord>(new CommandDefinition(
            $"""
            INSERT INTO scores (user_id, moves, pairs, elapsed_seconds)
            VALUES (@userId, @moves, @pairs, @elapsedSeconds);
            SELECT {SelectColumns} FROM scores WHERE id = LAST_INSERT_ID();
            """,
            new { userId, moves, pairs, elapsedSeconds },
            cancellationToken: ct));
    }

    // The user's own scores, newest first.
    public async Task<IReadOnlyList<ScoreRecord>> GetRecentAsync(long userId, int limit, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<ScoreRecord>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM scores WHERE user_id = @userId ORDER BY created_at DESC, id DESC LIMIT @limit",
            new { userId, limit },
            cancellationToken: ct));
        return rows.AsList();
    }

    // For each number of pairs, ordered by each user's fewest moves (fewer is better).
    public async Task<IReadOnlyList<RankingRow>> GetRankingAsync(int pairs, int limit, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<RankingRow>(new CommandDefinition(
            """
            SELECT u.login_id AS LoginId, MIN(s.moves) AS Moves
            FROM scores s
            JOIN users u ON u.id = s.user_id
            WHERE s.pairs = @pairs
            GROUP BY u.id, u.login_id
            ORDER BY Moves ASC, LoginId ASC
            LIMIT @limit
            """,
            new { pairs, limit },
            cancellationToken: ct));
        return rows.AsList();
    }
}
