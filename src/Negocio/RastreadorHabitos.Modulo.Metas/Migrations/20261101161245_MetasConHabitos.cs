using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Modulo.Metas.Migrations
{
    /// <inheritdoc />
    public partial class MetasConHabitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaInicio",
                schema: "Metas",
                table: "Metas",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "MetasHabitos",
                schema: "Metas",
                columns: table => new
                {
                    MetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HabitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasHabitos", x => new { x.MetaId, x.HabitoId });
                    table.ForeignKey(
                        name: "FK_MetasHabitos_Metas_MetaId",
                        column: x => x.MetaId,
                        principalSchema: "Metas",
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetasHabitos",
                schema: "Metas");

            migrationBuilder.DropColumn(
                name: "FechaInicio",
                schema: "Metas",
                table: "Metas");
        }
    }
}
