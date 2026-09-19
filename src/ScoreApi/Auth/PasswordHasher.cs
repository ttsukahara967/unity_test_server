using System.Security.Cryptography;

namespace ScoreApi.Auth;

// PBKDF2-SHA256. Stored format: pbkdf2-sha256$<iterations>$<salt(base64)>$<hash(base64)>
public static class PasswordHasher
{
    const string Prefix = "pbkdf2-sha256";
    const int SaltSize = 16;
    const int HashSize = 32;
    const int Iterations = 600_000;

    // Do the same amount of work when the user does not exist, so response time does not reveal whether an id exists.
    static readonly Lazy<string> DummyHash = new(() => Hash("dummy"));

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    // Always returns false when stored is null (no such user).
    public static bool Verify(string password, string? stored)
    {
        var parts = (stored ?? DummyHash.Value).Split('$');
        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected) && stored is not null;
    }
}
