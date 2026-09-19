namespace ScoreApi.Models;

public sealed record LoginRequest(string? Id, string? Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt);

public sealed record SubmitScoreRequest(int Moves, int Pairs, double? ElapsedSeconds);

public sealed record RankingEntry(int Rank, string Id, int Moves);
