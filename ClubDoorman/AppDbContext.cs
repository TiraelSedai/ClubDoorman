namespace ClubDoorman;

using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Stats> Stats { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // indexes
    }
}
