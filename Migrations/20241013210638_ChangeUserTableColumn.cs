using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaFisTransferEntegrasyonu.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserTableColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "password");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "password",
                table: "users",
                newName: "password_hash");
        }
    }
}
