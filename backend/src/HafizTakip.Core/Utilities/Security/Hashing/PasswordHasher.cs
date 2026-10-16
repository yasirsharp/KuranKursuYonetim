using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HafizTakip.Core.Utilities.Security.Hashing;

/// <summary>
/// PBKDF2-SHA256 parola özeti. Saklama biçimi: <c>pbkdf2-sha256$iterasyon$tuz(base64)$özet(base64)</c>
/// (tek metin kolonuna sığar; iterasyon sayısı ileride artırılsa da eski özetler doğrulanabilir).
/// </summary>
public static class PasswordHasher
{
    private const string Scheme = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    public const int DefaultIterations = 210_000;

    public static string Hash(string password, int iterations = DefaultIterations)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, HashSize);
        return string.Join('$', Scheme, iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>Özet tanınmayan biçimdeyse (ör. seed'deki yer tutucu) false döner.</summary>
    public static bool Verify(string password, string? stored)
    {
        var parts = stored?.Split('$');
        if (parts is not { Length: 4 } || parts[0] != Scheme
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations < 1)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
