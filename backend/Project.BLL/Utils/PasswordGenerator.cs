using System.Security.Cryptography;

namespace Project.BLL.Utils;

/// <summary>
/// Generates cryptographically secure random passwords.
/// Excludes visually ambiguous characters: O, I, l, 0, 1
/// to make printed credentials easier to read.
/// </summary>
public static class PasswordGenerator
{
    private static readonly char[] Upper  = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] Lower  = "abcdefghjkmnpqrstuvwxyz".ToCharArray();
    private static readonly char[] Digits = "23456789".ToCharArray();
    private static readonly char[] All    = [.. Upper, .. Lower, .. Digits];

    /// <summary>
    /// Generates a secure random password of the given length (default 10).
    /// Guarantees at least one uppercase, one lowercase, and one digit.
    /// </summary>
    public static string Generate(int length = 10)
    {
        if (length < 3)
            throw new ArgumentException("Password length must be at least 3.", nameof(length));

        var chars = new char[length];

        // Guarantee at least one of each required character type
        chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];

        // Fill the rest from the full charset
        for (var i = 3; i < length; i++)
            chars[i] = All[RandomNumberGenerator.GetInt32(All.Length)];

        // Cryptographically secure Fisher-Yates shuffle
        RandomNumberGenerator.Shuffle(chars.AsSpan());

        return new string(chars);
    }
}
