using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VisaoDeAguia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSinaisEnviados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SinaisEnviados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Simbolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Direcao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Forca = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Pontuacao = table.Column<int>(type: "integer", nullable: false),
                    Preco = table.Column<decimal>(type: "numeric", nullable: false),
                    DataHoraVela = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinaisEnviados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinaisEnviados_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SinaisEnviados_UsuarioId_Simbolo_Direcao_DataHoraVela",
                table: "SinaisEnviados",
                columns: new[] { "UsuarioId", "Simbolo", "Direcao", "DataHoraVela" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SinaisEnviados");
        }
    }
}
