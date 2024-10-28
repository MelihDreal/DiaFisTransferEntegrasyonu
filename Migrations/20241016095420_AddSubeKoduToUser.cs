using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaFisTransferEntegrasyonu.Migrations
{
    /// <inheritdoc />
    public partial class AddSubeKoduToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "api_url",
                table: "users",
                newName: "sube_kodu");

            migrationBuilder.AddColumn<string>(
                name: "SubeKodu",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubeKodu",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "sube_kodu",
                table: "users",
                newName: "api_url");
        }
    }
}
