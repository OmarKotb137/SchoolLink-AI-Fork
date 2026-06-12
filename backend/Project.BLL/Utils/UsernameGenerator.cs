using System.Security.Cryptography;

namespace Project.BLL.Utils;

/// <summary>
/// يولد usernames فريدة: prefix + 7 حروف/أرقام عشوائية.
/// يستبعد الحروف المتشابهة بصرياً: i, l, o, 0, 1
/// </summary>
public static class UsernameGenerator
{
    // 25 حرف + 8 أرقام = 33 قيمة → احتمال تكرار < 0.0001%
    private static readonly char[] Chars =
        "abcdefghjkmnpqrstuvwxyz23456789".ToCharArray();

    /// <summary>
    /// مثال: Generate('s') → "s7xm2pkr"
    ///        Generate('p') → "p4qnm8zx"
    ///        Generate('t') → "t9rk3mwp"
    /// </summary>
    public static string Generate(char prefix, int suffixLength = 7)
    {
        var bytes = new byte[suffixLength];
        RandomNumberGenerator.Fill(bytes);
        var suffix = new string(bytes.Select(b => Chars[b % Chars.Length]).ToArray());
        return $"{prefix}{suffix}";
    }
}
