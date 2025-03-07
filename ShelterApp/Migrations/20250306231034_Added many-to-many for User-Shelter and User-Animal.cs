using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Migrations
{
    /// <inheritdoc />
    public partial class AddedmanytomanyforUserShelterandUserAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalUser",
                columns: table => new
                {
                    AnimalsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalUser", x => new { x.AnimalsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_AnimalUser_Animals_AnimalsId",
                        column: x => x.AnimalsId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnimalUser_AspNetUsers_UsersId",
                        column: x => x.UsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShelterUser",
                columns: table => new
                {
                    SheltersId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelterUser", x => new { x.SheltersId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_ShelterUser_AspNetUsers_UsersId",
                        column: x => x.UsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShelterUser_Shelters_SheltersId",
                        column: x => x.SheltersId,
                        principalTable: "Shelters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalUser_UsersId",
                table: "AnimalUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterUser_UsersId",
                table: "ShelterUser",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalUser");

            migrationBuilder.DropTable(
                name: "ShelterUser");
        }
    }
}
