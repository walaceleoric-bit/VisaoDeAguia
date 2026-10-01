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
        private readonly IUltimaAnaliseService _ultimaAnaliseService;

        private TimeZoneInfo? _fusoHorario;

        public RoboMercadoBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RoboMercadoBackgroundService> logger,
            IUltimaAnaliseService ultimaAnaliseService)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
            _ultimaAnaliseService = ultimaAnaliseService;
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

                    // O robô não analisa aos sábados e domingos.
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

                    // O serviço permanece ativo e verifica
                    // o fechamento de cada bloco de 5 minutos.
                    //
                    // O horário permitido para análise é
                    // definido globalmente no appsettings.json.
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


        // ==========================================
        // FUSO HORÁRIO
        // ==========================================

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
                // Compatibilidade com Windows.
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


        // ==========================================
        // HORÁRIO GLOBAL DO ROBÔ
        // ==========================================

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

        private static bool EstaDentroDoHorario(
            TimeSpan agora,
            TimeSpan inicio,
            TimeSpan fim)
        {
            if (inicio <= fim)
            {
                return
                    agora >= inicio &&
                    agora < fim;
            }

            // Também permite períodos atravessando meia-noite.
            return
                agora >= inicio ||
                agora < fim;
        }


        // ==========================================
        // PRÓXIMA EXECUÇÃO
        // ==========================================

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

            // Pequena margem para garantir que
            // a vela de 5 minutos já esteja fechada
            // e disponível na API.
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


        // ==========================================
        // EXECUÇÃO DAS ANÁLISES
        // ==========================================

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
                    .GetRequiredService<IAnaliseMercadoService>();

            var telegramService =
                scope.ServiceProvider
                    .GetRequiredService<ITelegramService>();


            // ==========================================
            // HORÁRIO GLOBAL
            // ==========================================

            var horarioInicio =
                ObterHorario(
                    "RoboMercado:HorarioInicio",
                    TimeSpan.FromHours(6.5));

            var horarioFim =
                ObterHorario(
                    "RoboMercado:HorarioFim",
                    TimeSpan.FromHours(12));

            var agoraLocal =
                ObterAgoraLocal();

            if (!EstaDentroDoHorario(
                    agoraLocal.TimeOfDay,
                    horarioInicio,
                    horarioFim))
            {
                _logger.LogInformation(
                    "Robô fora do horário global de análise. Horário configurado: {Inicio} - {Fim}. Agora: {Agora}.",
                    horarioInicio,
                    horarioFim,
                    agoraLocal.ToString(
                        "dd/MM/yyyy HH:mm:ss"));

                return;
            }


            // ==========================================
            // ATIVO ANALISADO
            // ==========================================
            //
            // A análise acontece independentemente
            // de existir usuário com Telegram ativo.
            //
            // Assim, a página Analisar sempre poderá
            // receber a última análise feita pelo robô.
            // ==========================================

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

                // Guarda sempre a análise mais recente
                // realizada pelo robô.
                //
                // Isso inclui COMPRAR, VENDER e AGUARDAR.
                //
                // A página Analisar consulta este resultado
                // sem fazer uma nova chamada à Twelve Data.
                _ultimaAnaliseService.Atualizar(
                    resultado);

                _logger.LogInformation(
                    "Última análise de {Simbolo} atualizada na memória.",
                    resultado.Simbolo);

                _logger.LogInformation(
                    "Análise concluída. {Simbolo} - {Direcao} - {Forca} - {Pontuacao}/100.",
                    resultado.Simbolo,
                    resultado.Direcao,
                    resultado.Forca,
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


            // ==========================================
            // SOMENTE SINAIS CONFIRMADOS
            // ==========================================

            if (resultado.Direcao != "COMPRAR" &&
                resultado.Direcao != "VENDER")
            {
                _logger.LogInformation(
                    "Nenhum sinal confirmado para {Simbolo}.",
                    resultado.Simbolo);

                return;
            }

            if (resultado.Forca != "FORTE" &&
                resultado.Forca != "MODERADO")
            {
                _logger.LogInformation(
                    "Sinal de {Simbolo} sem força mínima para envio.",
                    resultado.Simbolo);

                return;
            }


            // ==========================================
            // USUÁRIOS COM TELEGRAM CONFIGURADO
            // ==========================================
            //
            // Somente depois da análise e da confirmação
            // do sinal buscamos quem deve recebê-lo.
            //
            // A ausência de Telegram não impede mais
            // a análise automática.
            // ==========================================

            var configuracoes =
                await context.ConfiguracoesRobo
                    .AsNoTracking()
                    .Where(c =>
                        c.TelegramAtivo &&
                        c.AnalisarForex &&
                        !string.IsNullOrWhiteSpace(
                            c.TelegramBotToken) &&
                        !string.IsNullOrWhiteSpace(
                            c.TelegramChatId))
                    .ToListAsync(
                        cancellationToken);

            if (configuracoes.Count == 0)
            {
                _logger.LogInformation(
                    "Análise realizada, mas não há configurações ativas de Telegram para receber o sinal.");

                return;
            }


            // ==========================================
            // ENVIO PARA OS USUÁRIOS
            // ==========================================

            foreach (var configuracao in configuracoes)
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


        // ==========================================
        // PROCESSAMENTO POR USUÁRIO
        // ==========================================

        private async Task ProcessarUsuarioAsync(
            AppDbContext context,
            ITelegramService telegramService,
            ConfiguracaoRobo configuracao,
            ResultadoAnalise resultado,
            CancellationToken cancellationToken)
        {
            // O robô atualmente trabalha somente
            // com Forex.
            if (!configuracao.AnalisarForex)
                return;


            // ==========================================
            // VALIDAÇÃO DO SINAL
            // ==========================================
            //
            // Não usamos mais:
            //
            // PontuacaoMinima
            // ReceberSinalForte
            // ReceberSinalModerado
            //
            // A classificação agora é definida
            // globalmente pela estratégia configurada
            // no appsettings.json.
            // ==========================================

            if (resultado.Direcao != "COMPRAR" &&
                resultado.Direcao != "VENDER")
            {
                return;
            }

            if (resultado.Forca != "FORTE" &&
                resultado.Forca != "MODERADO")
            {
                return;
            }


            // ==========================================
            // IDENTIFICAÇÃO DA VELA
            // ==========================================

            var dataHoraVelaUtc =
                ConverterHorarioLocalParaUtc(
                    resultado.DataHora);


            // ==========================================
            // PROTEÇÃO CONTRA DUPLICIDADE
            // ==========================================

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
                    "Sinal já enviado anteriormente para {UsuarioId}. Duplicação ignorada.",
                    configuracao.UsuarioId);

                return;
            }


            // ==========================================
            // MENSAGEM TELEGRAM
            // ==========================================

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


            // ==========================================
            // REGISTRO DO SINAL
            // ==========================================

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


        // ==========================================
        // CONVERSÃO DE HORÁRIO
        // ==========================================

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


        // ==========================================
        // MENSAGEM DO TELEGRAM
        // ==========================================

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


            // ==========================================
            // DISTÂNCIA DO NÍVEL CONTRÁRIO
            // ==========================================

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
                resultado.PrecoAtual *
                0.001m;

            var nivelProximo =
                distanciaNivelContrario <=
                limiteNivelProximo;

            var leituraRegiao =
                nivelProximo
                    ? direcaoCompra
                        ? "🟡 Resistência próxima"
                        : "🟡 Suporte próximo"
                    : "🟢 Espaço até o nível contrário";


            // ==========================================
            // CLASSIFICAÇÃO
            // ==========================================

            var classificacao =
                nivelProximo
                    ? "🟡 ATENÇÃO"
                    : resultado.Forca == "FORTE"
                        ? "🟢 FAVORÁVEL"
                        : "🟡 ATENÇÃO";

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
                    "🟢 Alinhadas para compra";
            }
            else if (resultado.MediasConfirmamVenda)
            {
                leituraMedias =
                    "🔴 Alinhadas para venda";
            }
            else
            {
                leituraMedias =
                    "🟡 Sem alinhamento completo";
            }


            // ==========================================
            // RSI
            // ==========================================

            var leituraRsi =
                resultado.SituacaoRsi switch
                {
                    "Compra" =>
                        "🟢 Compra",

                    "Venda" =>
                        "🔴 Venda",

                    _ =>
                        "🟡 Neutro"
                };


            // ==========================================
            // MACD
            // ==========================================

            var leituraMacd =
                resultado.SituacaoMacd switch
                {
                    "Compra" =>
                        "🟢 Compra",

                    "Venda" =>
                        "🔴 Venda",

                    _ =>
                        "🟡 Neutro"
                };


            // ==========================================
            // BOLLINGER
            // ==========================================

            var leituraBollinger =
                resultado.SituacaoBollinger5M switch
                {
                    "Compra" =>
                        "🟢 Compra",

                    "Venda" =>
                        "🔴 Venda",

                    _ =>
                        "🟡 Neutro"
                };


            // ==========================================
            // MENSAGEM FINAL
            // ==========================================

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


                "📐 INDICADORES\n" +
                $"〽️ Médias 10/50/200: {leituraMedias}\n" +
                $"📊 RSI 5M: {resultado.Rsi14_5M:F1}\n" +
                $"📊 RSI 15M: {resultado.Rsi14_15M:F1}\n" +
                $"➡️ RSI: {leituraRsi}\n" +
                $"📉 MACD: {leituraMacd}\n" +
                $"📶 Bollinger: {leituraBollinger}\n\n" +


                "📍 MÉDIAS 5M\n" +
                $"MA10: {resultado.Media10_5M:0.########}\n" +
                $"MA50: {resultado.Media50_5M:0.########}\n" +
                $"MA200: {resultado.Media200_5M:0.########}\n\n" +


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