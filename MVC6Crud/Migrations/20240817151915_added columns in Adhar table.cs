using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVC6Crud.Migrations
{
    /// <inheritdoc />
    public partial class addedcolumnsinAdhartable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdharAdress",
                table: "adharVerifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdharName",
                table: "adharVerifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "BackImage",
                table: "adharVerifications",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "FrontImage",
                table: "adharVerifications",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PanImage",
                table: "adharVerifications",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "PanNumber",
                table: "adharVerifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdharAdress",
                table: "adharVerifications");

            migrationBuilder.DropColumn(
                name: "AdharName",
                table: "adharVerifications");

            migrationBuilder.DropColumn(
                name: "BackImage",
                table: "adharVerifications");

            migrationBuilder.DropColumn(
                name: "FrontImage",
                table: "adharVerifications");

            migrationBuilder.DropColumn(
                name: "PanImage",
                table: "adharVerifications");

            migrationBuilder.DropColumn(
                name: "PanNumber",
                table: "adharVerifications");
        }
    }
}
