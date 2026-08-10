using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVC6Crud.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JSONtext",
                table: "payOutTransectionDetails");

            migrationBuilder.DropColumn(
                name: "JsonEncrypted",
                table: "payOutTransectionDetails");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "payOutTransectionDetails");

            migrationBuilder.DropColumn(
                name: "SefexRequest",
                table: "payOutTransectionDetails");

            migrationBuilder.DropColumn(
                name: "bankRefNo",
                table: "payOutTransectionDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "PayInCommission",
                table: "payIns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayInCommission",
                table: "payIns");

            migrationBuilder.AddColumn<string>(
                name: "JSONtext",
                table: "payOutTransectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JsonEncrypted",
                table: "payOutTransectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "payOutTransectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SefexRequest",
                table: "payOutTransectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bankRefNo",
                table: "payOutTransectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
