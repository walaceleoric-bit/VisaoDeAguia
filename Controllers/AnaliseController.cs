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
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnaliseController> _logger;
        private readonly IUltimaAnaliseService _ultimaAnaliseService;

        private TimeZoneInfo? _fusoHorario;

        public AnaliseController(
            IAnaliseMercadoService analiseMercadoService,
            ITelegramService telegramService,
            AppDbContext context,
            UserManager<Usuario> userManager,
            IConfiguration configuration,
            ILogger<AnaliseController> logger,
            IUltimaAnaliseService ultimaAnaliseService)
        {
            _analiseMercadoService = analiseMercadoService;
            _telegramService = telegramService;
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _ultimaAnaliseService = ultimaAnaliseService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                // ==========================================
                // ÚLTIMA ANÁLISE AUTOMÁTICA DO ROBÔ
                // ==========================================
                //
                // IMPORTANTE:
                //
                // A página Analisar não executa mais
                // uma nova consulta à Twelve Data.
                //
                // Ela apenas exibe a última análise
                // realizada automaticamente pelo robô.
                //
                // Portanto:
                //
                // - clicar em Analisar não consome API;
                // - atualizar a página não consome API;
                // - vários usuários podem visualizar
                //   a mesma análise sem novas consultas.
                // ==========================================

                var resultado =
                    _ultimaAnaliseService.Obter();

                if (resultado != null)
                {
                    _logger.LogInformation(
                        "Última análise automática exibida na tela. {Simbolo} - {Direcao} - {Forca} - {Pontuacao}/100 - Vela: {DataHora}.",
                        resultado.Simbolo,
                        resultado.Direcao,
                        resultado.Forca,
                        resultado.Pontuacao,
                        resultado.DataHora);

                    return View(resultado);
                }


                // ==========================================
                // AINDA NÃO EXISTE ANÁLISE NA MEMÓRIA
                // ==========================================
                //
                // Isso pode acontecer após:
                //
                // - iniciar a aplicação;
                // - fazer um novo deploy;
                // - reiniciar o Railway;
                // - antes do próximo ciclo de 5 minutos;
                // - fora do horário de funcionamento.
                //
                // Não fazemos uma análise manual como
                // fallback para evitar consumo adicional
                // da Twelve Data.
                // ==========================================

                ViewBag.Erro =
                    "Ainda não existe uma análise automática disponível. " +
                    "Aguarde a próxima análise realizada pelo robô.";

                return View(
                    new ResultadoAnalise
                    {
                        Simbolo = "EUR/USD",
                        Direcao = "AGUARDAR",
                        Forca = "SEM SINAL",
                        Pontuacao = 0
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao carregar a última análise automática.");

                ViewBag.Erro =
                    "Não foi possível carregar a última análise automática.";

                return View(
                    new ResultadoAnalise
                    {
                        Simbolo = "EUR/USD",
                        Direcao = "AGUARDAR",
                        Forca = "SEM SINAL",
                        Pontuacao = 0
                    });
            }
        }


        // ==========================================
        // ENVIO MANUAL PARA O TELEGRAM
        // ==========================================
        //
        // Este método foi mantido para preservar
        // a estrutura original do controller.
        //
        // O Index não chama mais este método.
        //
        // O envio automático dos sinais continua
        // sendo responsabilidade do robô.
        // ==========================================

        private async Task TentarEnviarTelegramAsync(
            ResultadoAnalise resultado)
        {
            try
            {
                // Somente sinais efetivamente confirmados.
                if (resultado.Direcao != "COMPRAR" &&
                    resultado.Direcao != "VENDER")
                {
                    return;
                }

                // A força mínima agora é determinada
                // globalmente pela estratégia.
                if (resultado.Forca != "FORTE" &&
                    resultado.Forca != "MODERADO")
                {
                    return;
                }

                var usuarioId =
                    _userManager.GetUserId(User);

                if (string.IsNullOrWhiteSpace(
                        usuarioId))
                {
                    return;
                }

                var configuracao =
                    await _context.ConfiguracoesRobo
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            c =>
                                c.UsuarioId ==
                                usuarioId);

                if (configuracao == null)
                    return;

                if (!configuracao.TelegramAtivo)
                    return;

                if (!configuracao.AnalisarForex)
                    return;

                if (string.IsNullOrWhiteSpace(
                        configuracao.TelegramBotToken) ||
                    string.IsNullOrWhiteSpace(
                        configuracao.TelegramChatId))
                {
                    return;
                }


                // ==========================================
                // HORÁRIO DA VELA EM UTC
                // ==========================================

                var dataHoraVelaUtc =
                    ConverterHorarioLocalParaUtc(
                        resultado.DataHora);


                // ==========================================
                // PROTEÇÃO CONTRA DUPLICIDADE
                // ==========================================

                var sinalJaEnviado =
                    await _context.SinaisEnviados
                        .AsNoTracking()
                        .AnyAsync(
                            s =>
                                s.UsuarioId ==
                                    usuarioId &&
                                s.Simbolo ==
                                    resultado.Simbolo &&
                                s.Direcao ==
                                    resultado.Direcao &&
                                s.DataHoraVela ==
                                    dataHoraVelaUtc);

                if (sinalJaEnviado)
                {
                    _logger.LogInformation(
                        "Sinal duplicado ignorado. Usuário: {UsuarioId}, Ativo: {Simbolo}, Direção: {Direcao}, Vela: {DataHoraVela}.",
                        usuarioId,
                        resultado.Simbolo,
                        resultado.Direcao,
                        dataHoraVelaUtc);

                    return;
                }


                // ==========================================
                // MENSAGEM
                // ==========================================

                var mensagem =
                    MontarMensagemTelegram(
                        resultado);

                var envio =
                    await _telegramService
                        .EnviarMensagemAsync(
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


                // ==========================================
                // REGISTRO DO SINAL
                // ==========================================

                var sinalEnviado =
                    new SinalEnviado
                    {
                        UsuarioId =
                            usuarioId,

                        Simbolo =
                            resultado.Simbolo,

                        Direcao =
                            resultado.Direcao,

                        Forca =
                            resultado.Forca,

                        Pontuacao =
                            resultado.Pontuacao,

                        Preco =
                            resultado.PrecoAtual,

                        DataHoraVela =
                            dataHoraVelaUtc,

                        DataEnvio =
                            DateTime.UtcNow
                    };

                _context.SinaisEnviados.Add(
                    sinalEnviado);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Sinal {Direcao} de {Simbolo} enviado ao Telegram e registrado no histórico.",
                        resultado.Direcao,
                        resultado.Simbolo);
                }
                catch (DbUpdateException ex)
                {
                    _context.Entry(
                        sinalEnviado).State =
                        EntityState.Detached;

                    _logger.LogWarning(
                        ex,
                        "Sinal duplicado ignorado ao salvar o histórico.");
                }
            }
            catch (Exception ex)
            {
                // Uma falha no Telegram ou histórico
                // não impede a análise de aparecer na tela.
                _logger.LogError(
                    ex,
                    "Erro durante o envio do sinal ao Telegram.");
            }
        }


        // ==========================================
        // FUSO HORÁRIO
        // ==========================================

        private TimeZoneInfo ObterFusoHorario()
        {
            if (_fusoHorario != null)
                return _fusoHorario;

            var fusoConfigurado =
                _configuration[
                    "RoboMercado:FusoHorario"];

            if (string.IsNullOrWhiteSpace(
                    fusoConfigurado))
            {
                fusoConfigurado =
                    "America/Sao_Paulo";
            }

            try
            {
                _fusoHorario =
                    TimeZoneInfo
                        .FindSystemTimeZoneById(
                            fusoConfigurado);
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    _fusoHorario =
                        TimeZoneInfo
                            .FindSystemTimeZoneById(
                                "E. South America Standard Time");
                }
                catch
                {
                    _logger.LogWarning(
                        "Não foi possível localizar o fuso {Fuso}. UTC será utilizado.",
                        fusoConfigurado);

                    _fusoHorario =
                        TimeZoneInfo.Utc;
                }
            }
            catch (InvalidTimeZoneException)
            {
                _logger.LogWarning(
                    "O fuso horário {Fuso} é inválido. UTC será utilizado.",
                    fusoConfigurado);

                _fusoHorario =
                    TimeZoneInfo.Utc;
            }

            return _fusoHorario;
        }

        private DateTime ConverterHorarioLocalParaUtc(
            DateTime horarioLocal)
        {
            var fuso =
                ObterFusoHorario();

            var horarioSemFuso =
                DateTime.SpecifyKind(
                    horarioLocal,
                    DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(
                horarioSemFuso,
                fuso);
        }


        // ==========================================
        // MENSAGEM DO TELEGRAM
        // ==========================================

        private static string MontarMensagemTelegram(
            ResultadoAnalise resultado)
        {
            var mensagem =
                new StringBuilder();

            var direcaoCompra =
                string.Equals(
                    resultado.Direcao,
                    "COMPRAR",
                    StringComparison.OrdinalIgnoreCase);

            var emojiDirecao =
                direcaoCompra
                    ? "🟢"
                    : "🔴";


            // ==========================================
            // MÉDIAS
            // ==========================================

            string leituraMedias;

            if (resultado.MediasConfirmamCompra)
            {
                leituraMedias =
                    "🟢 COMPRA";
            }
            else if (resultado.MediasConfirmamVenda)
            {
                leituraMedias =
                    "🔴 VENDA";
            }
            else
            {
                leituraMedias =
                    "🟡 NEUTRO";
            }


            // ==========================================
            // RSI
            // ==========================================

            var leituraRsi =
                resultado.SituacaoRsi switch
                {
                    "Compra" =>
                        "🟢 COMPRA",

                    "Venda" =>
                        "🔴 VENDA",

                    _ =>
                        "🟡 NEUTRO"
                };


            // ==========================================
            // MACD
            // ==========================================

            var leituraMacd =
                resultado.SituacaoMacd switch
                {
                    "Compra" =>
                        "🟢 COMPRA",

                    "Venda" =>
                        "🔴 VENDA",

                    _ =>
                        "🟡 NEUTRO"
                };


            // ==========================================
            // BOLLINGER
            // ==========================================

            var leituraBollinger =
                resultado.SituacaoBollinger5M switch
                {
                    "Compra" =>
                        "🟢 COMPRA",

                    "Venda" =>
                        "🔴 VENDA",

                    _ =>
                        "🟡 NEUTRO"
                };


            // ==========================================
            // CABEÇALHO
            // ==========================================

            mensagem.AppendLine(
                "🦅 VISÃO DE ÁGUIA");

            mensagem.AppendLine();

            mensagem.AppendLine(
                $"{emojiDirecao} SINAL {resultado.Simbolo}");

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


            // ==========================================
            // CONTEXTO
            // ==========================================

            mensagem.AppendLine(
                "📊 CONTEXTO");

            mensagem.AppendLine(
                $"📈 2H: {resultado.Tendencia2H}");

            mensagem.AppendLine(
                $"📈 1H: {resultado.Tendencia1H}");

            mensagem.AppendLine(
                $"📊 30M: {resultado.Estrutura30M}");

            mensagem.AppendLine(
                $"🔄 15M: {resultado.Pullback15M}");

            mensagem.AppendLine(
                $"⚡ 5M: {resultado.Confirmacao5M}");

            mensagem.AppendLine();


            // ==========================================
            // INDICADORES
            // ==========================================

            mensagem.AppendLine(
                "📐 INDICADORES");

            mensagem.AppendLine(
                $"〽️ Médias 10/50/200: {leituraMedias}");

            mensagem.AppendLine(
                $"📊 RSI 5M: {resultado.Rsi14_5M:0.00}");

            mensagem.AppendLine(
                $"📊 RSI 15M: {resultado.Rsi14_15M:0.00}");

            mensagem.AppendLine(
                $"➡️ RSI: {leituraRsi}");

            mensagem.AppendLine(
                $"📉 MACD: {leituraMacd}");

            mensagem.AppendLine(
                $"📶 Bollinger: {leituraBollinger}");

            mensagem.AppendLine();


            // ==========================================
            // MÉDIAS 5M
            // ==========================================

            mensagem.AppendLine(
                "📍 MÉDIAS 5M");

            mensagem.AppendLine(
                $"EMA 10: {resultado.Media10_5M:0.########}");

            mensagem.AppendLine(
                $"EMA 50: {resultado.Media50_5M:0.########}");

            mensagem.AppendLine(
                $"EMA 200: {resultado.Media200_5M:0.########}");

            mensagem.AppendLine();


            // ==========================================
            // NÍVEIS
            // ==========================================

            mensagem.AppendLine(
                "🎯 NÍVEIS");

            mensagem.AppendLine(
                $"🛡️ Suporte: {resultado.Suporte:0.########}");

            mensagem.AppendLine(
                $"🚧 Resistência: {resultado.Resistencia:0.########}");

            mensagem.AppendLine();


            // ==========================================
            // HORÁRIO
            // ==========================================

            mensagem.AppendLine(
                $"🕐 Vela: {resultado.DataHora:dd/MM/yyyy HH:mm}");

            mensagem.AppendLine();

            mensagem.AppendLine(
                "⚠️ Sinal gerado pelo Visão de Águia.");

            return mensagem.ToString();
        }
    }
}