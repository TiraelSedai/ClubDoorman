namespace ClubDoorman;

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Stats> Stats { get; init; }
    public DbSet<BlacklistedUser> BlacklistedUsers { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // indexes
    }
}

public sealed class BlacklistedUser
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }
}
