using Dapper;
using MySqlConnector;
using ScoreApi.Auth;

namespace ScoreApi.Data;

// On startup, creates the tables (if missing) and seeds the development user.
public sealed class DatabaseInitializer(
    MySqlDataSource dataSource,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger)
{
    const int MaxAttempts = 30;

    static readonly string[] Schema =
    [
        """
        CREATE TABLE IF NOT EXISTS users (
          id BIGINT AUTO_INCREMENT PRIMARY KEY,
          login_id VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
          password_hash VARCHAR(255) NOT NULL,
          created_at DATETIME(3) NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
          UNIQUE KEY uq_users_login_id (login_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """,
        """
        CREATE TABLE IF NOT EXISTS scores (
          id BIGINT AUTO_INCREMENT PRIMARY KEY,
          user_id BIGINT NOT NULL,
          moves INT NOT NULL,
          pairs INT NOT NULL,
          elapsed_seconds DOUBLE NULL,
          created_at DATETIME(3) NOT NULL DEFAULT (UTC_TIMESTAMP(3)),
          INDEX idx_scores_ranking (pairs, moves),
          INDEX idx_scores_user (user_id, created_at),
          CONSTRAINT fk_scores_user FOREIGN KEY (user_id) REFERENCES users (id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """,
    ];

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenWithRetryAsync(ct);
        foreach (var statement in Schema)
            await connection.ExecuteAsync(new CommandDefinition(statement, cancellationToken: ct));
        await SeedUserAsync(connection, ct);
    }

    // The API can come up before the MySQL container is ready, so wait until we can connect.
    async Task<MySqlConnection> OpenWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await dataSource.OpenConnectionAsync(ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts && (ex is MySqlException or IOException))
            {
                logger.LogWarning("Cannot connect to MySQL. Retrying ({Attempt}/{Max})", attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    async Task SeedUserAsync(MySqlConnection connection, CancellationToken ct)
    {
        var loginId = configuration["Seed:LoginId"];
        var password = configuration["Seed:Password"];
        if (string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(password))
            return;

        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM users WHERE login_id = @loginId", new { loginId }, cancellationToken: ct));
        if (count > 0)
            return;

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO users (login_id, password_hash) VALUES (@loginId, @hash)",
            new { loginId, hash = PasswordHasher.Hash(password) },
            cancellationToken: ct));
        logger.LogInformation("Created development user {LoginId}", loginId);
    }
}
