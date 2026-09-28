using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VisaoDeAguia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarConfiguracaoRobo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesRobo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    TelegramBotToken = table.Column<string>(type: "text", nullable: true),
                    TelegramChatId = table.Column<string>(type: "text", nullable: true),
                    TelegramAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    AnalisarForex = table.Column<bool>(type: "boolean", nullable: false),
                    AnalisarAcoes = table.Column<bool>(type: "boolean", nullable: false),
                    AnalisarCriptomoedas = table.Column<bool>(type: "boolean", nullable: false),
                    ReceberSinalForte = table.Column<bool>(type: "boolean", nullable: false),
                    ReceberSinalModerado = table.Column<bool>(type: "boolean", nullable: false),
                    PontuacaoMinima = table.Column<int>(type: "integer", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesRobo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesRobo_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesRobo_UsuarioId",
                table: "ConfiguracoesRobo",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesRobo");
        }
    }
}
