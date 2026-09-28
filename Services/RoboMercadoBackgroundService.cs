using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Data;
using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public class RoboMercadoBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoboMercadoBackgroundService> _logger;

        private TimeZoneInfo? _fusoHorario;

        public RoboMercadoBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RoboMercadoBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _fusoHorario = ObterFusoHorario();

            _logger.LogInformation(
                "Robô Visão de Águia iniciado. Fuso horário: {Fuso}.",
                _fusoHorario.Id);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var agoraLocal = ObterAgoraLocal();

                    // O robô continua sem analisar aos sábados e domingos.
                    if (!EhDiaUtil(agoraLocal))
                    {
                        _logger.LogInformation(
                            "Robô pausado no fim de semana. Agora: {Agora}.",
                            agoraLocal.ToString(
                                "dd/MM/yyyy HH:mm:ss"));

                        await Task.Delay(
                            TimeSpan.FromMinutes(5),
                            stoppingToken);

                        continue;
                    }

                    // O serviço fica ativo o dia inteiro, mas acorda
                    // somente no fechamento de cada bloco de 5 minutos.
                    // O horário individual de cada usuário é validado
                    // em ExecutarAnalisesAsync antes de consumir a API.
                    var tempoEspera =
                        CalcularTempoAteProximaExecucao(
                            agoraLocal);

                    var proximaExecucaoLocal =
                        agoraLocal.Add(
                            tempoEspera);

                    _logger.LogInformation(
                        "Horário de Brasília: {Agora}. Próxima verificação: {Proxima}.",
                        agoraLocal.ToString(
                            "dd/MM/yyyy HH:mm:ss"),
                        proximaExecucaoLocal.ToString(
                            "dd/MM/yyyy HH:mm:ss"));

                    await Task.Delay(
                        tempoEspera,
                        stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    agoraLocal = ObterAgoraLocal();

                    if (!EhDiaUtil(agoraLocal))
                        continue;

                    await ExecutarAnalisesAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro durante execução automática do robô.");

                    await Task.Delay(
                        TimeSpan.FromSeconds(30),
                        stoppingToken);
                }
            }

            _logger.LogInformation(
                "Robô Visão de Águia finalizado.");
        }

        private TimeZoneInfo ObterFusoHorario()
        {
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
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        fusoConfigurado);
            }
            catch (TimeZoneNotFoundException)
            {
                // Compatibilidade com ambientes Windows
                // que eventualmente não reconheçam o ID IANA.
                try
                {
                    return TimeZoneInfo
                        .FindSystemTimeZoneById(
                            "E. South America Standard Time");
                }
                catch
                {
                    _logger.LogWarning(
                        "Não foi possível localizar o fuso {Fuso}. UTC será utilizado.",
                        fusoConfigurado);

                    return TimeZoneInfo.Utc;
                }
            }
            catch (InvalidTimeZoneException)
            {
                _logger.LogWarning(
                    "O fuso horário {Fuso} é inválido. UTC será utilizado.",
                    fusoConfigurado);

                return TimeZoneInfo.Utc;
            }
        }

        private DateTime ObterAgoraLocal()
        {
            var fuso =
                _fusoHorario ??
                ObterFusoHorario();

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                fuso);
        }

        private TimeSpan ObterHorario(
            string chave,
            TimeSpan valorPadrao)
        {
            var valor =
                _configuration[chave];

            if (TimeSpan.TryParse(
                    valor,
                    out var horario))
            {
                return horario;
            }

            _logger.LogWarning(
                "Horário inválido em {Chave}. Será utilizado {HorarioPadrao}.",
                chave,
                valorPadrao);

            return valorPadrao;
        }

        private static bool EhDiaUtil(
            DateTime dataHora)
        {
            return
                dataHora.DayOfWeek !=
                    DayOfWeek.Saturday &&
                dataHora.DayOfWeek !=
                    DayOfWeek.Sunday;
        }

        private static TimeSpan
            CalcularTempoAteProximoDiaUtil(
                DateTime agoraLocal,
                TimeSpan horarioInicio)
        {
            var proximaData =
                agoraLocal.Date.AddDays(1);

            while (
                proximaData.DayOfWeek ==
                    DayOfWeek.Saturday ||
                proximaData.DayOfWeek ==
                    DayOfWeek.Sunday)
            {
                proximaData =
                    proximaData.AddDays(1);
            }

            var proximaAtivacao =
                proximaData.Add(
                    horarioInicio);

            return proximaAtivacao -
                   agoraLocal;
        }

        private static bool EstaDentroDoHorario(
            TimeSpan agora,
            TimeSpan inicio,
            TimeSpan fim)
        {
            if (inicio <= fim)
            {
                return agora >= inicio &&
                       agora < fim;
            }

            // Também permite períodos
            // atravessando meia-noite.
            return agora >= inicio ||
                   agora < fim;
        }

        private static TimeSpan
            CalcularTempoAteInicio(
                DateTime agoraLocal,
                TimeSpan horarioInicio)
        {
            var proximoInicio =
                agoraLocal.Date.Add(
                    horarioInicio);

            if (proximoInicio <= agoraLocal)
            {
                proximoInicio =
                    proximoInicio.AddDays(1);

                while (
                    proximoInicio.DayOfWeek ==
                        DayOfWeek.Saturday ||
                    proximoInicio.DayOfWeek ==
                        DayOfWeek.Sunday)
                {
                    proximoInicio =
                        proximoInicio.AddDays(1);
                }
            }

            return proximoInicio -
                   agoraLocal;
        }

        private static TimeSpan
            CalcularTempoAteProximaExecucao(
                DateTime agoraLocal)
        {
            var minutosProximoBloco =
                ((agoraLocal.Minute / 5) + 1) * 5;

            DateTime proximaExecucao;

            if (minutosProximoBloco >= 60)
            {
                proximaExecucao =
                    new DateTime(
                        agoraLocal.Year,
                        agoraLocal.Month,
                        agoraLocal.Day,
                        agoraLocal.Hour,
                        0,
                        0)
                    .AddHours(1);
            }
            else
            {
                proximaExecucao =
                    new DateTime(
                        agoraLocal.Year,
                        agoraLocal.Month,
                        agoraLocal.Day,
                        agoraLocal.Hour,
                        minutosProximoBloco,
                        0);
            }

            // Margem para a vela de 5M estar
            // fechada e disponível na API.
            proximaExecucao =
                proximaExecucao.AddSeconds(5);

            var espera =
                proximaExecucao -
                agoraLocal;

            if (espera <= TimeSpan.Zero)
            {
                return TimeSpan.FromSeconds(5);
            }

            return espera;
        }

        private async Task ExecutarAnalisesAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var analiseMercadoService =
                scope.ServiceProvider
                    .GetRequiredService<
                        IAnaliseMercadoService>();

            var telegramService =
                scope.ServiceProvider
                    .GetRequiredService<
                        ITelegramService>();

            var configuracoes =
                await context.ConfiguracoesRobo
                    .AsNoTracking()
                    .Where(c =>
                        c.TelegramAtivo &&
                        !string.IsNullOrWhiteSpace(
                            c.TelegramBotToken) &&
                        !string.IsNullOrWhiteSpace(
                            c.TelegramChatId))
                    .ToListAsync(
                        cancellationToken);

            if (configuracoes.Count == 0)
            {
                _logger.LogInformation(
                    "Nenhuma configuração ativa encontrada para o robô.");

                return;
            }

            var agoraLocal =
                ObterAgoraLocal();

            // Mantém somente os usuários cujo período escolhido
            // inclui o horário atual de Brasília.
            configuracoes =
                configuracoes
                    .Where(c =>
                        EstaDentroDoHorario(
                            agoraLocal.TimeOfDay,
                            c.HorarioInicio,
                            c.HorarioFim))
                    .ToList();

            if (configuracoes.Count == 0)
            {
                _logger.LogInformation(
                    "Nenhum usuário está dentro do horário de análise neste momento. Agora: {Agora}.",
                    agoraLocal.ToString(
                        "dd/MM/yyyy HH:mm:ss"));

                return;
            }

            const string simbolo =
                "EUR/USD";

            ResultadoAnalise resultado;

            try
            {
                _logger.LogInformation(
                    "Iniciando análise automática de {Simbolo}.",
                    simbolo);

                resultado =
                    await analiseMercadoService
                        .AnalisarAsync(
                            simbolo);

                _logger.LogInformation(
                    "Análise concluída. {Simbolo} - {Direcao} - {Pontuacao}/100.",
                    resultado.Simbolo,
                    resultado.Direcao,
                    resultado.Pontuacao);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao analisar automaticamente {Simbolo}.",
                    simbolo);

                return;
            }

            if (resultado.Direcao != "COMPRAR" &&
                resultado.Direcao != "VENDER")
            {
                _logger.LogInformation(
                    "Nenhum sinal confirmado para {Simbolo}.",
                    resultado.Simbolo);

                return;
            }

            foreach (
                var configuracao in configuracoes)
            {
                if (cancellationToken
                    .IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await ProcessarUsuarioAsync(
                        context,
                        telegramService,
                        configuracao,
                        resultado,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro ao processar sinal automático para o usuário {UsuarioId}.",
                        configuracao.UsuarioId);
                }
            }
        }

        private async Task ProcessarUsuarioAsync(
            AppDbContext context,
            ITelegramService telegramService,
            ConfiguracaoRobo configuracao,
            ResultadoAnalise resultado,
            CancellationToken cancellationToken)
        {
            // Agora o robô trabalha somente com Forex.
            if (!configuracao.AnalisarForex)
                return;

            if (resultado.Pontuacao <
                configuracao.PontuacaoMinima)
            {
                return;
            }

            if (resultado.Forca == "FORTE" &&
                !configuracao.ReceberSinalForte)
            {
                return;
            }

            if (resultado.Forca == "MODERADO" &&
                !configuracao
                    .ReceberSinalModerado)
            {
                return;
            }

            if (resultado.Forca != "FORTE" &&
                resultado.Forca != "MODERADO")
            {
                return;
            }

            // Converte o horário da vela para UTC uma única vez.
            // Esse mesmo valor é usado na consulta e na gravação.
            var dataHoraVelaUtc =
                ConverterHorarioLocalParaUtc(
                    resultado.DataHora);

            var sinalJaEnviado =
                await context.SinaisEnviados
                    .AsNoTracking()
                    .AnyAsync(
                        s =>
                            s.UsuarioId ==
                                configuracao.UsuarioId &&
                            s.Simbolo ==
                                resultado.Simbolo &&
                            s.Direcao ==
                                resultado.Direcao &&
                            s.DataHoraVela ==
                                dataHoraVelaUtc,
                        cancellationToken);

            if (sinalJaEnviado)
            {
                _logger.LogInformation(
                    "Sinal já enviado anteriormente. Duplicação ignorada.");

                return;
            }

            var mensagem =
                MontarMensagemTelegram(
                    resultado);

            var envio =
                await telegramService
                    .EnviarMensagemAsync(
                        configuracao
                            .TelegramBotToken!,
                        configuracao
                            .TelegramChatId!,
                        mensagem);

            if (!envio.Sucesso)
            {
                _logger.LogWarning(
                    "Falha ao enviar sinal automático para {UsuarioId}: {Mensagem}",
                    configuracao.UsuarioId,
                    envio.Mensagem);

                return;
            }

            var sinal =
                new SinalEnviado
                {
                    UsuarioId =
                        configuracao.UsuarioId,

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

            context.SinaisEnviados.Add(
                sinal);

            try
            {
                await context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Sinal automático {Direcao} de {Simbolo} enviado para {UsuarioId}.",
                    resultado.Direcao,
                    resultado.Simbolo,
                    configuracao.UsuarioId);
            }
            catch (DbUpdateException ex)
            {
                context.Entry(sinal).State =
                    EntityState.Detached;

                _logger.LogWarning(
                    ex,
                    "Sinal automático duplicado ignorado.");
            }
        }

        private DateTime ConverterHorarioLocalParaUtc(
            DateTime horarioLocal)
        {
            var fuso =
                _fusoHorario ??
                ObterFusoHorario();

            var horarioSemFuso =
                DateTime.SpecifyKind(
                    horarioLocal,
                    DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(
                horarioSemFuso,
                fuso);
        }

        private static string
            MontarMensagemTelegram(
                ResultadoAnalise resultado)
        {
            var horarioVela =
                resultado.DataHora;

            var inicioReferencia =
                horarioVela.AddMinutes(5);

            var fimReferencia =
                inicioReferencia.AddMinutes(5);

            var direcaoCompra =
                string.Equals(
                    resultado.Direcao,
                    "COMPRAR",
                    StringComparison.OrdinalIgnoreCase);

            var distanciaSuporte =
                Math.Abs(
                    resultado.PrecoAtual -
                    resultado.Suporte);

            var distanciaResistencia =
                Math.Abs(
                    resultado.Resistencia -
                    resultado.PrecoAtual);

            var distanciaNivelContrario =
                direcaoCompra
                    ? distanciaResistencia
                    : distanciaSuporte;

            var limiteNivelProximo =
                resultado.PrecoAtual * 0.001m;

            var nivelProximo =
                distanciaNivelContrario <=
                limiteNivelProximo;

            var leituraRegiao =
                nivelProximo
                    ? direcaoCompra
                        ? "🟡 Resistência próxima"
                        : "🟡 Suporte próximo"
                    : "🟢 Espaço até o nível contrário";

            var classificacao =
                nivelProximo
                    ? "🟡 ATENÇÃO"
                    : resultado.Pontuacao >= 85
                        ? "🟢 FAVORÁVEL"
                        : "🟡 ATENÇÃO";

            var emojiDirecao =
                direcaoCompra
                    ? "🟢"
                    : "🔴";

            return
                "🦅 VISÃO DE ÁGUIA\n\n" +
                $"{emojiDirecao} {resultado.Simbolo} • {resultado.Direcao}\n" +
                $"⭐ {resultado.Forca} • {resultado.Pontuacao}/100\n" +
                $"💰 {resultado.PrecoAtual:0.########}\n\n" +

                "📊 CONTEXTO\n" +
                $"📈 2H: {resultado.Tendencia2H}\n" +
                $"📈 1H: {resultado.Tendencia1H}\n" +
                $"📊 30M: {resultado.Estrutura30M}\n" +
                $"🔄 15M: {resultado.Pullback15M}\n" +
                $"⚡ 5M: {resultado.Confirmacao5M}\n\n" +

                "🎯 NÍVEIS\n" +
                $"🛡️ Suporte: {resultado.Suporte:0.########}\n" +
                $"🚧 Resistência: {resultado.Resistencia:0.########}\n" +
                $"{leituraRegiao}\n\n" +

                "⏱️ TIMING\n" +
                $"🕐 M5: {horarioVela:HH:mm}\n" +
                $"⏳ Referência: {inicioReferencia:HH:mm} - {fimReferencia:HH:mm}\n\n" +

                $"🎯 {classificacao}";
        }
    }
}