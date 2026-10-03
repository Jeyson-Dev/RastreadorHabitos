using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Modulo.Metas.Migrations
{
    /// <inheritdoc />
    public partial class InicialMetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Metas");

            migrationBuilder.CreateTable(
                name: "Metas",
                schema: "Metas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ObjetivoCumplimientos = table.Column<int>(type: "int", nullable: false),
                    FechaLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metas", x => x.Id);
                    table.CheckConstraint("CK_Metas_ObjetivoCumplimientos", "[ObjetivoCumplimientos] >= 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Metas_UsuarioId",
                schema: "Metas",
                table: "Metas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Metas",
                schema: "Metas");
        }
    }
}
