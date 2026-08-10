using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVC6Crud.Migrations
{
    /// <inheritdoc />
    public partial class srrenablecolumnadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SrrEnable",
                table: "payManGateWayMarigins",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SrrEnable",
                table: "payManGateWayMarigins");
        }
    }
}
