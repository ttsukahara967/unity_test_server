using Dapper;
using MySqlConnector;

namespace ScoreApi.Data;

public sealed record UserRecord(long Id, string LoginId, string PasswordHash);

public sealed class UserRepository(MySqlDataSource dataSource)
{
    public async Task<UserRecord?> FindByLoginIdAsync(string loginId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UserRecord>(new CommandDefinition(
            "SELECT id AS Id, login_id AS LoginId, password_hash AS PasswordHash FROM users WHERE login_id = @loginId",
            new { loginId },
            cancellationToken: ct));
    }
}
