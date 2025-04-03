using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalFieldsAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersAnimal_Animals_AnimalId",
                table: "UsersAnimal");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersAnimal_AspNetUsers_UserId",
                table: "UsersAnimal");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersShelter_AspNetUsers_UserId",
                table: "UsersShelter");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersShelter_Shelters_ShelterId",
                table: "UsersShelter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsersShelter",
                table: "UsersShelter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsersAnimal",
                table: "UsersAnimal");

            migrationBuilder.DropColumn(
                name: "PhotoURL",
                table: "Animals");

            migrationBuilder.RenameTable(
                name: "UsersShelter",
                newName: "UsersShelters");

            migrationBuilder.RenameTable(
                name: "UsersAnimal",
                newName: "UsersAnimals");

            migrationBuilder.RenameIndex(
                name: "IX_UsersShelter_UserId",
                table: "UsersShelters",
                newName: "IX_UsersShelters_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UsersAnimal_UserId",
                table: "UsersAnimals",
                newName: "IX_UsersAnimals_UserId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Animals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HealthCondition",
                table: "Animals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "Animals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Animals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Sterilized",
                table: "Animals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsersShelters",
                table: "UsersShelters",
                columns: new[] { "ShelterId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsersAnimals",
                table: "UsersAnimals",
                columns: new[] { "AnimalId", "UserId" });

            migrationBuilder.CreateTable(
                name: "AnimalPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserLastModified = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnimalPhotos_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShelterFeedbacks",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ShelterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelterFeedbacks", x => new { x.UserId, x.ShelterId });
                    table.ForeignKey(
                        name: "FK_ShelterFeedbacks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShelterFeedbacks_Shelters_ShelterId",
                        column: x => x.ShelterId,
                        principalTable: "Shelters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_AnimalId",
                table: "AnimalPhotos",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterFeedbacks_ShelterId",
                table: "ShelterFeedbacks",
                column: "ShelterId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsersAnimals_Animals_AnimalId",
                table: "UsersAnimals",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersAnimals_AspNetUsers_UserId",
                table: "UsersAnimals",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersShelters_AspNetUsers_UserId",
                table: "UsersShelters",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersShelters_Shelters_ShelterId",
                table: "UsersShelters",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersAnimals_Animals_AnimalId",
                table: "UsersAnimals");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersAnimals_AspNetUsers_UserId",
                table: "UsersAnimals");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersShelters_AspNetUsers_UserId",
                table: "UsersShelters");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersShelters_Shelters_ShelterId",
                table: "UsersShelters");

            migrationBuilder.DropTable(
                name: "AnimalPhotos");

            migrationBuilder.DropTable(
                name: "ShelterFeedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsersShelters",
                table: "UsersShelters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsersAnimals",
                table: "UsersAnimals");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "HealthCondition",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "Sterilized",
                table: "Animals");

            migrationBuilder.RenameTable(
                name: "UsersShelters",
                newName: "UsersShelter");

            migrationBuilder.RenameTable(
                name: "UsersAnimals",
                newName: "UsersAnimal");

            migrationBuilder.RenameIndex(
                name: "IX_UsersShelters_UserId",
                table: "UsersShelter",
                newName: "IX_UsersShelter_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UsersAnimals_UserId",
                table: "UsersAnimal",
                newName: "IX_UsersAnimal_UserId");

            migrationBuilder.AddColumn<string>(
                name: "PhotoURL",
                table: "Animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsersShelter",
                table: "UsersShelter",
                columns: new[] { "ShelterId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsersAnimal",
                table: "UsersAnimal",
                columns: new[] { "AnimalId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UsersAnimal_Animals_AnimalId",
                table: "UsersAnimal",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersAnimal_AspNetUsers_UserId",
                table: "UsersAnimal",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersShelter_AspNetUsers_UserId",
                table: "UsersShelter",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersShelter_Shelters_ShelterId",
                table: "UsersShelter",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
