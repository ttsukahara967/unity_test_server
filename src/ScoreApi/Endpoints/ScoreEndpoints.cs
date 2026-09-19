using System.Security.Claims;
using ScoreApi.Data;
using ScoreApi.Models;

namespace ScoreApi.Endpoints;

public static class ScoreEndpoints
{
    // Leaves headroom above the game's own limit (8 pairs).
    const int MaxPairs = 32;
    const int MaxMoves = 10_000;
    const double MaxElapsedSeconds = 86_400;
    const int MaxLimit = 100;

    public static void MapScoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scores")
            .RequireAuthorization()
            .WithTags("Scores");

        group.MapPost("/", Submit)
            .WithSummary("Submit a score")
            .WithDescription($"Saves the score for the logged-in user. moves must be at least pairs, and pairs must be between 1 and {MaxPairs}. elapsedSeconds (in seconds) is optional.")
            .Produces<ScoreRecord>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetMine)
            .WithSummary("List my scores")
            .WithDescription("Returns the logged-in user's own scores, newest first. limit defaults to 20 (max 100).")
            .Produces<IReadOnlyList<ScoreRecord>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/ranking", GetRanking)
            .WithSummary("Ranking")
            .WithDescription("Ranks users by their fewest moves, separately for each number of pairs (pairs defaults to 8). limit defaults to 10 (max 100).")
            .Produces<IEnumerable<RankingEntry>>()
            .Produces(StatusCodes.Status401Unauthorized);
    }

    static async Task<IResult> Submit(
        SubmitScoreRequest request, ClaimsPrincipal principal, ScoreRepository scores, CancellationToken ct)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var saved = await scores.AddAsync(UserId(principal), request.Moves, request.Pairs, request.ElapsedSeconds, ct);
        return Results.Json(saved, statusCode: StatusCodes.Status201Created);
    }

    static async Task<IResult> GetMine(
        ClaimsPrincipal principal, ScoreRepository scores, CancellationToken ct, int? limit = null)
    {
        var rows = await scores.GetRecentAsync(UserId(principal), ClampLimit(limit, 20), ct);
        return Results.Ok(rows);
    }

    static async Task<IResult> GetRanking(
        ScoreRepository scores, CancellationToken ct, int? pairs = null, int? limit = null)
    {
        var rows = await scores.GetRankingAsync(pairs ?? 8, ClampLimit(limit, 10), ct);
        var entries = rows.Select((row, index) => new RankingEntry(index + 1, row.LoginId, row.Moves));
        return Results.Ok(entries);
    }

    // moves can never be less than the number of pairs (matching a pair takes at least one move).
    static Dictionary<string, string[]> Validate(SubmitScoreRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Pairs is < 1 or > MaxPairs)
            errors["pairs"] = [$"pairs must be between 1 and {MaxPairs}."];
        if (request.Moves < Math.Max(request.Pairs, 1) || request.Moves > MaxMoves)
            errors["moves"] = [$"moves must be at least pairs and at most {MaxMoves}."];
        if (request.ElapsedSeconds is < 0 or > MaxElapsedSeconds)
            errors["elapsedSeconds"] = [$"elapsedSeconds must be between 0 and {MaxElapsedSeconds}."];
        return errors;
    }

    static int ClampLimit(int? limit, int fallback) => Math.Clamp(limit ?? fallback, 1, MaxLimit);

    static long UserId(ClaimsPrincipal principal) =>
        long.Parse(principal.FindFirst("sub")?.Value ?? throw new InvalidOperationException("The sub claim is missing."));
}
