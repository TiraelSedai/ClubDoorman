using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubDoorman.Migrations
{
    /// <inheritdoc />
    public partial class NewStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "Autoban", table: "Stats", type: "INTEGER", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<int>(name: "Channels", table: "Stats", type: "INTEGER", nullable: false, defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Autoban", table: "Stats");

            migrationBuilder.DropColumn(name: "Channels", table: "Stats");
        }
    }
}
