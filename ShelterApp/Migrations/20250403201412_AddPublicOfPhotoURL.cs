using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicOfPhotoURL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoURL",
                table: "AnimalPhotos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoURL",
                table: "AnimalPhotos");
        }
    }
}
