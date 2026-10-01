using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Data;
using VisaoDeAguia.Models;
using VisaoDeAguia.Services;

namespace VisaoDeAguia.Controllers
{
    [Authorize]
    public class ConfiguracaoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ITelegramService _telegramService;

        public ConfiguracaoController(
            AppDbContext context,
            UserManager<Usuario> userManager,
            ITelegramService telegramService)
        {
            _context = context;
            _userManager = userManager;
            _telegramService = telegramService;
        }

        // ==========================================
        // CONFIGURAÇÃO DO TELEGRAM
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarioId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            var configuracao =
                await _context.ConfiguracoesRobo
                    .FirstOrDefaultAsync(
                        c => c.UsuarioId == usuarioId);

            if (configuracao == null)
            {
                configuracao =
                    new ConfiguracaoRobo
                    {
                        UsuarioId = usuarioId,

                        TelegramAtivo = false,

                        AnalisarForex = true,
                        AnalisarAcoes = false,
                        AnalisarCriptomoedas = false,

                        ReceberSinalForte = true,
                        ReceberSinalModerado = true,

                        PontuacaoMinima = 0,

                        HorarioInicio =
                            new TimeSpan(8, 30, 0),

                        HorarioFim =
                            new TimeSpan(11, 0, 0)
                    };
            }
            else
            {
                // Por segurança, nunca mostramos
                // novamente o token salvo.
                configuracao.TelegramBotToken = null;
            }

            return View(configuracao);
        }

        // ==========================================
        // SALVAR TELEGRAM
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            ConfiguracaoRobo model)
        {
            var usuarioId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            var configuracao =
                await _context.ConfiguracoesRobo
                    .FirstOrDefaultAsync(
                        c => c.UsuarioId == usuarioId);

            var tokenInformado =
                !string.IsNullOrWhiteSpace(
                    model.TelegramBotToken);

            var tokenJaSalvo =
                configuracao != null &&
                !string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken);

            if (!tokenInformado &&
                !tokenJaSalvo)
            {
                TempData["ErroTelegram"] =
                    "Informe o Token do Bot do Telegram.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (configuracao == null)
            {
                configuracao =
                    new ConfiguracaoRobo
                    {
                        UsuarioId = usuarioId,

                        TelegramAtivo = false,

                        AnalisarForex = true,
                        AnalisarAcoes = false,
                        AnalisarCriptomoedas = false,

                        ReceberSinalForte = true,
                        ReceberSinalModerado = true,

                        PontuacaoMinima = 0,

                        HorarioInicio =
                            new TimeSpan(8, 30, 0),

                        HorarioFim =
                            new TimeSpan(11, 0, 0)
                    };

                _context.ConfiguracoesRobo.Add(
                    configuracao);
            }

            // Só troca o token quando o usuário
            // realmente informar um novo.
            if (tokenInformado)
            {
                configuracao.TelegramBotToken =
                    model.TelegramBotToken!.Trim();
            }

            // O Chat ID pode ser preenchido
            // automaticamente pela busca do grupo.
            if (!string.IsNullOrWhiteSpace(
                    model.TelegramChatId))
            {
                configuracao.TelegramChatId =
                    model.TelegramChatId.Trim();
            }

            // Se já existe Chat ID, o Telegram
            // pode ficar ativo.
            configuracao.TelegramAtivo =
                !string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken) &&
                !string.IsNullOrWhiteSpace(
                    configuracao.TelegramChatId);

            // Mantemos o EUR/USD habilitado.
            configuracao.AnalisarForex = true;

            configuracao.DataAtualizacao =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                string.IsNullOrWhiteSpace(
                    configuracao.TelegramChatId)
                    ? "Token do Telegram salvo. Agora adicione o bot ao grupo, envie uma mensagem no grupo e clique em Buscar Grupo."
                    : "Configuração do Telegram salva com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // ==========================================
        // BUSCAR GRUPO AUTOMATICAMENTE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuscarGrupo()
        {
            var usuarioId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            var configuracao =
                await _context.ConfiguracoesRobo
                    .FirstOrDefaultAsync(
                        c => c.UsuarioId == usuarioId);

            if (configuracao == null ||
                string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken))
            {
                TempData["ErroTelegram"] =
                    "Primeiro informe o Token do Bot e salve.";

                return RedirectToAction(
                    nameof(Index));
            }

            var grupos =
                await _telegramService
                    .BuscarGruposAsync(
                        configuracao.TelegramBotToken);

            if (grupos.Count == 0)
            {
                TempData["ErroTelegram"] =
                    "Nenhum grupo foi encontrado. Adicione o bot ao grupo, envie uma mensagem no grupo e tente novamente.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (grupos.Count == 1)
            {
                configuracao.TelegramChatId =
                    grupos[0].ChatId.ToString();

                configuracao.TelegramAtivo = true;

                configuracao.DataAtualizacao =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    $"Grupo \"{grupos[0].Nome}\" encontrado. Telegram configurado e ativado automaticamente.";

                return RedirectToAction(
                    nameof(Index));
            }

            TempData["GruposTelegram"] =
                grupos
                    .Select(
                        g =>
                            $"{g.Nome} | {g.ChatId}")
                    .ToArray();

            TempData["ErroTelegram"] =
                "Mais de um grupo foi encontrado. Deixe o bot somente no grupo desejado e tente novamente.";

            return RedirectToAction(
                nameof(Index));
        }

        // ==========================================
        // TESTAR TELEGRAM
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestarTelegram()
        {
            var usuarioId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            var configuracao =
                await _context.ConfiguracoesRobo
                    .FirstOrDefaultAsync(
                        c => c.UsuarioId == usuarioId);

            if (configuracao == null)
            {
                TempData["ErroTelegram"] =
                    "Configuração do Telegram não encontrada.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken))
            {
                TempData["ErroTelegram"] =
                    "O Token do Bot não está configurado.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    configuracao.TelegramChatId))
            {
                TempData["ErroTelegram"] =
                    "O grupo do Telegram ainda não foi localizado.";

                return RedirectToAction(
                    nameof(Index));
            }

            var mensagem =
                "🦅 VISÃO DE ÁGUIA\n\n" +
                "✅ Integração com Telegram configurada com sucesso.\n\n" +
                "🤖 O sistema está pronto para enviar sinais.";

            var resultado =
                await _telegramService
                    .EnviarMensagemAsync(
                        configuracao.TelegramBotToken,
                        configuracao.TelegramChatId,
                        mensagem);

            if (resultado.Sucesso)
            {
                configuracao.TelegramAtivo = true;

                configuracao.DataAtualizacao =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    "Mensagem de teste enviada para o Telegram com sucesso.";
            }
            else
            {
                TempData["ErroTelegram"] =
                    resultado.Mensagem;
            }

            return RedirectToAction(
                nameof(Index));
        }
    }
}