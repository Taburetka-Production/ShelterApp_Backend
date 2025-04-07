﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Migrations
{
    /// <inheritdoc />
    public partial class AddedSlugtoShelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Shelters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Shelters");
        }
    }
}
