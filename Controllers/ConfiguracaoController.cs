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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Conta");

            var configuracao = await _context.ConfiguracoesRobo
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (configuracao == null)
            {
                configuracao = new ConfiguracaoRobo
                {
                    UsuarioId = usuarioId,
                    TelegramAtivo = false,
                    AnalisarForex = true,
                    AnalisarAcoes = true,
                    AnalisarCriptomoedas = true,
                    ReceberSinalForte = true,
                    ReceberSinalModerado = false,
                    PontuacaoMinima = 80
                };
            }
            else
            {
                // Não envia o Token salvo para a tela.
                // O campo fica vazio por segurança.
                configuracao.TelegramBotToken = null;
            }

            return View(configuracao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConfiguracaoRobo model)
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Conta");

            model.UsuarioId = usuarioId;

            ModelState.Remove(nameof(model.UsuarioId));
            ModelState.Remove(nameof(model.Usuario));

            var configuracao = await _context.ConfiguracoesRobo
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            var tokenInformado =
                !string.IsNullOrWhiteSpace(model.TelegramBotToken);

            var tokenJaSalvo =
                configuracao != null &&
                !string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken);

            var chatIdInformado =
                !string.IsNullOrWhiteSpace(model.TelegramChatId);

            if (model.PontuacaoMinima < 0 ||
                model.PontuacaoMinima > 100)
            {
                ModelState.AddModelError(
                    nameof(model.PontuacaoMinima),
                    "A pontuação mínima deve estar entre 0 e 100.");
            }

            if (model.TelegramAtivo)
            {
                if (!tokenInformado &&
                    !tokenJaSalvo)
                {
                    ModelState.AddModelError(
                        nameof(model.TelegramBotToken),
                        "Informe o Token do Bot para ativar o Telegram.");
                }

                if (!chatIdInformado)
                {
                    ModelState.AddModelError(
                        nameof(model.TelegramChatId),
                        "Informe o Chat ID para ativar o Telegram.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Nunca devolvemos o Token salvo para a tela.
                model.TelegramBotToken = null;

                return View(model);
            }

            if (configuracao == null)
            {
                configuracao = new ConfiguracaoRobo
                {
                    UsuarioId = usuarioId
                };

                _context.ConfiguracoesRobo.Add(configuracao);
            }

            // Só altera o Token se o usuário realmente
            // informar um novo Token.
            if (tokenInformado)
            {
                configuracao.TelegramBotToken =
                    model.TelegramBotToken!.Trim();
            }

            configuracao.TelegramChatId =
                model.TelegramChatId?.Trim();

            configuracao.TelegramAtivo =
                model.TelegramAtivo;

            configuracao.AnalisarForex =
                model.AnalisarForex;

            configuracao.AnalisarAcoes =
                model.AnalisarAcoes;

            configuracao.AnalisarCriptomoedas =
                model.AnalisarCriptomoedas;

            configuracao.ReceberSinalForte =
                model.ReceberSinalForte;

            configuracao.ReceberSinalModerado =
                model.ReceberSinalModerado;

            configuracao.PontuacaoMinima =
                model.PontuacaoMinima;

            configuracao.DataAtualizacao =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Configurações salvas com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuscarGrupo()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Conta");

            var configuracao = await _context.ConfiguracoesRobo
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (configuracao == null ||
                string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken))
            {
                TempData["ErroTelegram"] =
                    "Primeiro informe o Token do Bot e salve as configurações.";

                return RedirectToAction(nameof(Index));
            }

            var grupos =
                await _telegramService.BuscarGruposAsync(
                    configuracao.TelegramBotToken);

            if (grupos.Count == 0)
            {
                TempData["ErroTelegram"] =
                    "Nenhum grupo foi encontrado. Adicione o bot ao grupo e envie uma mensagem no grupo.";

                return RedirectToAction(nameof(Index));
            }

            if (grupos.Count == 1)
            {
                configuracao.TelegramChatId =
                    grupos[0].ChatId.ToString();

                configuracao.DataAtualizacao =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    $"Grupo \"{grupos[0].Nome}\" encontrado. Chat ID configurado automaticamente.";

                return RedirectToAction(nameof(Index));
            }

            TempData["GruposTelegram"] =
                grupos
                    .Select(
                        g => $"{g.Nome} | {g.ChatId}")
                    .ToArray();

            TempData["ErroTelegram"] =
                "Mais de um grupo foi encontrado. Vamos adicionar a seleção de grupo na próxima etapa.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestarTelegram()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Conta");

            var configuracao =
                await _context.ConfiguracoesRobo
                    .FirstOrDefaultAsync(
                        c => c.UsuarioId == usuarioId);

            if (configuracao == null)
            {
                TempData["ErroTelegram"] =
                    "Configuração do Telegram não encontrada.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    configuracao.TelegramBotToken))
            {
                TempData["ErroTelegram"] =
                    "O Token do Bot não está configurado.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    configuracao.TelegramChatId))
            {
                TempData["ErroTelegram"] =
                    "O Chat ID do grupo não está configurado.";

                return RedirectToAction(nameof(Index));
            }

            var mensagem =
                "🦅 VISÃO DE ÁGUIA\n\n" +
                "✅ Integração com Telegram configurada com sucesso.\n\n" +
                "🤖 O sistema está pronto para enviar sinais.";

            var resultado =
                await _telegramService.EnviarMensagemAsync(
                    configuracao.TelegramBotToken,
                    configuracao.TelegramChatId,
                    mensagem);

            if (resultado.Sucesso)
            {
                TempData["Sucesso"] =
                    "Mensagem de teste enviada para o Telegram com sucesso.";
            }
            else
            {
                TempData["ErroTelegram"] =
                    resultado.Mensagem;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}