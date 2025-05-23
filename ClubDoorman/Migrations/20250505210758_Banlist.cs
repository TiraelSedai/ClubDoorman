using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubDoorman.Migrations
{
    /// <inheritdoc />
    public partial class Banlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlacklistedUsers",
                columns: table => new { Id = table.Column<long>(type: "INTEGER", nullable: false) },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedUsers", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BlacklistedUsers");
        }
    }
}
