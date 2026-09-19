using ScoreApi.Auth;
using ScoreApi.Data;
using ScoreApi.Models;

namespace ScoreApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/login", Login)
            .WithTags("Auth")
            .WithSummary("Log in")
            .WithDescription("Authenticates with an id and password and returns a JWT. The token is valid for 60 minutes. The default development user is user1 / pass.")
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    static async Task<IResult> Login(
        LoginRequest request, UserRepository users, TokenService tokens, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Id) || string.IsNullOrEmpty(request.Password))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "id and password are required.");

        var user = await users.FindByLoginIdAsync(request.Id, ct);
        if (!PasswordHasher.Verify(request.Password, user?.PasswordHash) || user is null)
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid id or password.");

        var (token, expiresAt) = tokens.CreateToken(user.Id, user.LoginId);
        return Results.Ok(new LoginResponse(token, expiresAt));
    }
}
