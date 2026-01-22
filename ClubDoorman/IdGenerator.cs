namespace ClubDoorman;

internal static class IdGenerator
{
    private static readonly IdGen.IdGenerator Generator = new(0);
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public static string NextBase62() => ToBase62(Generator.CreateId());

    private static string ToBase62(long value) =>
        string.Create(11, value, static (span, val) =>
        {
            for (var i = span.Length - 1; i >= 0; i--)
            {
                span[i] = Base62Chars[(int)(val % 62)];
                val /= 62;
            }
        });
}
