using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using VisaoDeAguia.Data;
using VisaoDeAguia.Models;
using VisaoDeAguia.Services;

namespace VisaoDeAguia.Controllers
{
    [Authorize]
    public class AnaliseController : Controller
    {
        private readonly IAnaliseMercadoService _analiseMercadoService;
        private readonly ITelegramService _telegramService;
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<AnaliseController> _logger;

        public AnaliseController(
            IAnaliseMercadoService analiseMercadoService,
            ITelegramService telegramService,
            AppDbContext context,
            UserManager<Usuario> userManager,
            ILogger<AnaliseController> logger)
        {
            _analiseMercadoService = analiseMercadoService;
            _telegramService = telegramService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var resultado =
                    await _analiseMercadoService.AnalisarAsync(
                        "EUR/USD");

                await TentarEnviarTelegramAsync(resultado);

                return View(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao executar análise de mercado.");

                ViewBag.Erro = ex.Message;

                return View(new ResultadoAnalise
                {
                    Simbolo = "EUR/USD",
                    Direcao = "AGUARDAR",
                    Forca = "SEM SINAL",
                    Pontuacao = 0
                });
            }
        }

        private async Task TentarEnviarTelegramAsync(
            ResultadoAnalise resultado)
        {
            try
            {
                // Não envia quando não existe sinal.
                if (resultado.Direcao != "COMPRAR" &&
                    resultado.Direcao != "VENDER")
                {
                    return;
                }

                var usuarioId = _userManager.GetUserId(User);

                if (string.IsNullOrWhiteSpace(usuarioId))
                    return;

                var configuracao =
                    await _context.ConfiguracoesRobo
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            c => c.UsuarioId == usuarioId);

                if (configuracao == null)
                    return;

                // Telegram precisa estar ativado.
                if (!configuracao.TelegramAtivo)
                    return;

                // Token e Chat ID precisam estar configurados.
                if (string.IsNullOrWhiteSpace(
                        configuracao.TelegramBotToken) ||
                    string.IsNullOrWhiteSpace(
                        configuracao.TelegramChatId))
                {
                    return;
                }

                // Respeita a pontuação mínima.
                if (resultado.Pontuacao <
                    configuracao.PontuacaoMinima)
                {
                    return;
                }

                // Respeita os tipos de sinais escolhidos.
                if (resultado.Forca == "FORTE" &&
                    !configuracao.ReceberSinalForte)
                {
                    return;
                }

                if (resultado.Forca == "MODERADO" &&
                    !configuracao.ReceberSinalModerado)
                {
                    return;
                }

                if (resultado.Forca != "FORTE" &&
                    resultado.Forca != "MODERADO")
                {
                    return;
                }

                // Verifica se este mesmo sinal desta mesma
                // vela já foi enviado para este usuário.
                var sinalJaEnviado =
                    await _context.SinaisEnviados
                        .AsNoTracking()
                        .AnyAsync(s =>
                            s.UsuarioId == usuarioId &&
                            s.Simbolo == resultado.Simbolo &&
                            s.Direcao == resultado.Direcao &&
                            s.DataHoraVela == resultado.DataHora);

                if (sinalJaEnviado)
                {
                    _logger.LogInformation(
                        "Sinal duplicado ignorado. Usuário: {UsuarioId}, Ativo: {Simbolo}, Direção: {Direcao}, Vela: {DataHoraVela}.",
                        usuarioId,
                        resultado.Simbolo,
                        resultado.Direcao,
                        resultado.DataHora);

                    return;
                }

                var mensagem =
                    MontarMensagemTelegram(resultado);

                var envio =
                    await _telegramService.EnviarMensagemAsync(
                        configuracao.TelegramBotToken,
                        configuracao.TelegramChatId,
                        mensagem);

                if (!envio.Sucesso)
                {
                    _logger.LogWarning(
                        "Não foi possível enviar sinal ao Telegram: {Mensagem}",
                        envio.Mensagem);

                    return;
                }

                // Só registra no banco depois que o Telegram
                // confirmar que a mensagem foi enviada.
                var sinalEnviado = new SinalEnviado
                {
                    UsuarioId = usuarioId,
                    Simbolo = resultado.Simbolo,
                    Direcao = resultado.Direcao,
                    Forca = resultado.Forca,
                    Pontuacao = resultado.Pontuacao,
                    Preco = resultado.PrecoAtual,
                    DataHoraVela = resultado.DataHora,
                    DataEnvio = DateTime.UtcNow
                };

                _context.SinaisEnviados.Add(sinalEnviado);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Sinal {Direcao} de {Simbolo} enviado ao Telegram e registrado no histórico.",
                    resultado.Direcao,
                    resultado.Simbolo);
            }
            catch (DbUpdateException ex)
            {
                // O índice único do banco também protege
                // contra tentativas simultâneas de duplicação.
                _logger.LogWarning(
                    ex,
                    "O sinal já foi registrado ou ocorreu conflito ao salvar o histórico.");
            }
            catch (Exception ex)
            {
                // Uma falha no Telegram ou no histórico não deve
                // impedir que a análise apareça na tela.
                _logger.LogError(
                    ex,
                    "Erro durante o envio do sinal ao Telegram.");
            }
        }

        private static string MontarMensagemTelegram(
            ResultadoAnalise resultado)
        {
            var mensagem = new StringBuilder();

            mensagem.AppendLine("🦅 VISÃO DE ÁGUIA");
            mensagem.AppendLine();

            mensagem.AppendLine(
                $"🚨 SINAL {resultado.Simbolo}");

            mensagem.AppendLine();

            mensagem.AppendLine(
                $"📊 Direção: {resultado.Direcao}");

            mensagem.AppendLine(
                $"⭐ Força: {resultado.Forca}");

            mensagem.AppendLine(
                $"🎯 Pontuação: {resultado.Pontuacao}/100");

            mensagem.AppendLine(
                $"💰 Preço: {resultado.PrecoAtual:0.########}");

            mensagem.AppendLine();

            mensagem.AppendLine(
                $"📈 Tendência 2H: {resultado.Tendencia2H}");

            mensagem.AppendLine(
                $"📈 Tendência 1H: {resultado.Tendencia1H}");

            mensagem.AppendLine(
                $"📊 Estrutura 30M: {resultado.Estrutura30M}");

            mensagem.AppendLine(
                $"🔄 Pullback 15M: {resultado.Pullback15M}");

            mensagem.AppendLine(
                $"⚡ Confirmação 5M: {resultado.Confirmacao5M}");

            mensagem.AppendLine();

            mensagem.AppendLine(
                $"🛡️ Suporte: {resultado.Suporte:0.########}");

            mensagem.AppendLine(
                $"🚧 Resistência: {resultado.Resistencia:0.########}");

            mensagem.AppendLine();

            mensagem.AppendLine(
                $"🕐 Vela: {resultado.DataHora:dd/MM/yyyy HH:mm}");

            mensagem.AppendLine();

            mensagem.AppendLine(
                "⚠️ Sinal gerado automaticamente pelo Visão de Águia.");

            return mensagem.ToString();
        }
    }
}