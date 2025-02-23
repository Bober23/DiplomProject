using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiplomProject.Backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImageFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentImagesLink",
                table: "Documents");

            migrationBuilder.CreateTable(
                name: "ImageFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LoadDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LinkToFile = table.Column<string>(type: "text", nullable: false),
                    Documentid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageFiles_Documents_Documentid",
                        column: x => x.Documentid,
                        principalTable: "Documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Documentid",
                table: "ImageFiles",
                column: "Documentid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageFiles");

            migrationBuilder.AddColumn<string>(
                name: "ContentImagesLink",
                table: "Documents",
                type: "text",
                nullable: true);
        }
    }
}
