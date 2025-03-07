using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Migrations
{
    /// <inheritdoc />
    public partial class addedshelterIdforuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShelterId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ShelterId",
                table: "AspNetUsers",
                column: "ShelterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Shelters_ShelterId",
                table: "AspNetUsers",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Shelters_ShelterId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ShelterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShelterId",
                table: "AspNetUsers");
        }
    }
}
