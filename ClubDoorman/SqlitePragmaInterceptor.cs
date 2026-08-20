namespace ClubDoorman;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// EF Core sets journal_mode=WAL and foreign_keys=ON itself, but leaves busy_timeout at 0 and
/// cache_size at SQLite's 2 MB default. Both are per-connection, so they have to be re-applied
/// every time EF opens one - setting them once at startup reaches nothing.
/// </summary>
internal sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    // No mmap_size on purpose: these boxes have little RAM and no swap. synchronous=NORMAL still
    // fsyncs at every WAL checkpoint, so a crash or an OOM kill loses nothing; only losing the
    // host itself can drop the last commits, and the file stays consistent either way.
    private const string Pragmas = "PRAGMA busy_timeout = 5000; PRAGMA cache_size = -20000; PRAGMA synchronous = NORMAL;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => Apply(connection);

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        Apply(connection);
        return Task.CompletedTask;
    }

    private static void Apply(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }
}
