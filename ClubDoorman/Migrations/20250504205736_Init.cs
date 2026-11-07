using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubDoorman.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    ChatTitle = table.Column<string>(type: "TEXT", nullable: true),
                    StoppedCaptcha = table.Column<int>(type: "INTEGER", nullable: false),
                    BlacklistBanned = table.Column<int>(type: "INTEGER", nullable: false),
                    KnownBadMessage = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Stats");
        }
    }
}
