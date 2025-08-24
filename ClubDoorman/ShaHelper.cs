using System.Security.Cryptography;
using System.Text;

namespace ClubDoorman;

internal static class ShaHelper
{
    public static string ComputeSha256Hex(string? input)
    {
        if (input == null)
            return "null";
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
