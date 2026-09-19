namespace ScoreApi.Auth;

public sealed class JwtOptions
{
    // Signing key. Supplied through the Jwt__Key environment variable (at least 32 bytes).
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "unity-test-server";
    public string Audience { get; set; } = "unity-test-game";
    public int ExpiresMinutes { get; set; } = 60;
}
