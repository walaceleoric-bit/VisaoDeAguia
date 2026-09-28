using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VisaoDeAguia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaControleConsumoDiario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumosDiariosRobo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MinutosUtilizados = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosDiariosRobo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumosDiariosRobo_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosDiariosRobo_UsuarioId_Data",
                table: "ConsumosDiariosRobo",
                columns: new[] { "UsuarioId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumosDiariosRobo");
        }
    }
}
