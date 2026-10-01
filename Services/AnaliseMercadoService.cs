using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public class AnaliseMercadoService : IAnaliseMercadoService
    {
        private readonly ITwelveDataService _twelveDataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnaliseMercadoService> _logger;

        public AnaliseMercadoService(
            ITwelveDataService twelveDataService,
            IConfiguration configuration,
            ILogger<AnaliseMercadoService> logger)
        {
            _twelveDataService = twelveDataService;
            _configuration = configuration;
            _logger = logger;
        }


        public async Task<ResultadoAnalise> AnalisarAsync(
            string simbolo)
        {
            try
            {
                // ==========================================
                // PARÂMETROS CONFIGURÁVEIS
                // ==========================================

                var mediaRapida =
                    ObterIntConfiguracao(
                        "Indicadores:MediaRapida",
                        10);

                var mediaIntermediaria =
                    ObterIntConfiguracao(
                        "Indicadores:MediaIntermediaria",
                        50);

                var mediaLonga =
                    ObterIntConfiguracao(
                        "Indicadores:MediaLonga",
                        200);

                var bollingerPeriodo =
                    ObterIntConfiguracao(
                        "Indicadores:BollingerPeriodo",
                        20);

                var bollingerDesvio =
                    ObterDecimalConfiguracao(
                        "Indicadores:BollingerDesvio",
                        2m);

                var rsiPeriodo =
                    ObterIntConfiguracao(
                        "Indicadores:RsiPeriodo",
                        14);

                var rsiSobrevendido =
                    ObterDecimalConfiguracao(
                        "Indicadores:RsiSobrevendido",
                        30m);

                var rsiSobrecomprado =
                    ObterDecimalConfiguracao(
                        "Indicadores:RsiSobrecomprado",
                        70m);

                var macdRapida =
                    ObterIntConfiguracao(
                        "Indicadores:MacdRapida",
                        12);

                var macdLenta =
                    ObterIntConfiguracao(
                        "Indicadores:MacdLenta",
                        26);

                var macdSinal =
                    ObterIntConfiguracao(
                        "Indicadores:MacdSinal",
                        9);

                var pontuacaoSinalForte =
                    ObterIntConfiguracao(
                        "Indicadores:PontuacaoSinalForte",
                        80);

                var pontuacaoSinalModerado =
                    ObterIntConfiguracao(
                        "Indicadores:PontuacaoSinalModerado",
                        60);


                // ==========================================
                // DADOS DE MERCADO
                // ==========================================

                // Continua fazendo apenas UMA consulta.
                // Os outros timeframes são construídos
                // internamente a partir das velas de 5M.
                var velas5M =
                    await _twelveDataService.ObterVelasAsync(
                        simbolo,
                        "5min",
                        5000);

                if (velas5M.Count < 1200)
                {
                    throw new InvalidOperationException(
                        "Quantidade de velas de 5 minutos insuficiente para realizar a análise.");
                }

                var velas15M =
                    AgruparVelasPorIntervalo(
                        velas5M,
                        15);

                var velas30M =
                    AgruparVelasPorIntervalo(
                        velas5M,
                        30);

                var velas1H =
                    AgruparVelasPorIntervalo(
                        velas5M,
                        60);

                var velas2H =
                    AgruparVelasPorIntervalo(
                        velas5M,
                        120);

                if (velas15M.Count < 200 ||
                    velas30M.Count < 100 ||
                    velas1H.Count < 50 ||
                    velas2H.Count < 20)
                {
                    throw new InvalidOperationException(
                        "Quantidade de velas agrupadas insuficiente para realizar a análise.");
                }


                // ==========================================
                // EMAs ANTIGAS
                // Mantidas por compatibilidade
                // ==========================================

                var ema10_2H =
                    CalcularEma(
                        velas2H,
                        10);

                var ema20_2H =
                    CalcularEma(
                        velas2H,
                        20);

                var ema10_1H =
                    CalcularEma(
                        velas1H,
                        10);

                var ema20_1H =
                    CalcularEma(
                        velas1H,
                        20);

                var ema10_30M =
                    CalcularEma(
                        velas30M,
                        10);

                var ema20_30M =
                    CalcularEma(
                        velas30M,
                        20);

                var ema10_15M =
                    CalcularEma(
                        velas15M,
                        10);

                var ema20_15M =
                    CalcularEma(
                        velas15M,
                        20);

                var ema10_5M =
                    CalcularEma(
                        velas5M,
                        10);

                var ema20_5M =
                    CalcularEma(
                        velas5M,
                        20);


                // ==========================================
                // MÉDIAS 10 / 50 / 200
                // ==========================================

                var media10_5M =
                    CalcularEma(
                        velas5M,
                        mediaRapida);

                var media50_5M =
                    CalcularEma(
                        velas5M,
                        mediaIntermediaria);

                var media200_5M =
                    CalcularEma(
                        velas5M,
                        mediaLonga);


                var media10_15M =
                    CalcularEma(
                        velas15M,
                        mediaRapida);

                var media50_15M =
                    CalcularEma(
                        velas15M,
                        mediaIntermediaria);

                var media200_15M =
                    CalcularEma(
                        velas15M,
                        mediaLonga);


                var media10_30M =
                    CalcularEma(
                        velas30M,
                        mediaRapida);

                var media50_30M =
                    CalcularEma(
                        velas30M,
                        mediaIntermediaria);

                var media200_30M =
                    CalcularEma(
                        velas30M,
                        mediaLonga);


                var media10_1H =
                    CalcularEma(
                        velas1H,
                        mediaRapida);

                var media50_1H =
                    CalcularEma(
                        velas1H,
                        mediaIntermediaria);

                var media200_1H =
                    CalcularEma(
                        velas1H,
                        mediaLonga);


                var media10_2H =
                    CalcularEma(
                        velas2H,
                        mediaRapida);

                var media50_2H =
                    CalcularEma(
                        velas2H,
                        mediaIntermediaria);

                var media200_2H =
                    CalcularEma(
                        velas2H,
                        mediaLonga);


                // ==========================================
                // TENDÊNCIAS PRINCIPAIS
                // ==========================================

                var tendencia2H =
                    AnalisarTendencia(
                        velas2H,
                        ema10_2H,
                        ema20_2H);

                var tendencia1H =
                    AnalisarTendencia(
                        velas1H,
                        ema10_1H,
                        ema20_1H);

                var estrutura30M =
                    AnalisarTendencia(
                        velas30M,
                        ema10_30M,
                        ema20_30M);


                // ==========================================
                // SUPORTE E RESISTÊNCIA
                // ==========================================

                var suporte =
                    CalcularSuporte(
                        velas30M,
                        20);

                var resistencia =
                    CalcularResistencia(
                        velas30M,
                        20);

                var precoAtual =
                    velas5M[^1].Fechamento;

                var proximoSuporte =
                    EstaProximo(
                        precoAtual,
                        suporte,
                        0.005m);

                var proximoResistencia =
                    EstaProximo(
                        precoAtual,
                        resistencia,
                        0.005m);


                // ==========================================
                // ALINHAMENTO MACRO
                // ==========================================

                var tendenciasAlinhadas =
                    (
                        tendencia2H == "Alta" &&
                        tendencia1H == "Alta" &&
                        estrutura30M == "Alta"
                    )
                    ||
                    (
                        tendencia2H == "Baixa" &&
                        tendencia1H == "Baixa" &&
                        estrutura30M == "Baixa"
                    );


                // ==========================================
                // PULLBACK 15M
                // ==========================================

                var pullback15M =
                    AnalisarPullback(
                        velas15M,
                        tendencia1H,
                        ema10_15M,
                        ema20_15M);

                var pullbackConfirmado =
                    pullback15M ==
                        "Confirmado para compra" ||
                    pullback15M ==
                        "Confirmado para venda";


                // ==========================================
                // GATILHO 5M
                // ==========================================

                var confirmacao5M =
                    AnalisarConfirmacao(
                        velas5M,
                        tendencia1H,
                        ema10_5M,
                        ema20_5M);

                var gatilhoConfirmado =
                    confirmacao5M == "Compra" ||
                    confirmacao5M == "Venda";


                // ==========================================
                // BOLLINGER 20,2
                // ==========================================

                var bollinger =
                    CalcularBollinger(
                        velas5M,
                        bollingerPeriodo,
                        bollingerDesvio);

                var situacaoBollinger =
                    AnalisarBollinger(
                        velas5M,
                        bollinger.Superior,
                        bollinger.Media,
                        bollinger.Inferior);


                // ==========================================
                // RSI 14
                // ==========================================

                var rsi5M =
                    CalcularRsi(
                        velas5M,
                        rsiPeriodo);

                var rsi15M =
                    CalcularRsi(
                        velas15M,
                        rsiPeriodo);

                var situacaoRsi =
                    AnalisarRsi(
                        rsi5M,
                        rsi15M,
                        rsiSobrevendido,
                        rsiSobrecomprado);


                // ==========================================
                // MACD 12,26,9
                // ==========================================

                var macd =
                    CalcularMacd(
                        velas5M,
                        macdRapida,
                        macdLenta,
                        macdSinal);

                var situacaoMacd =
                    AnalisarMacd(
                        macd.Macd,
                        macd.Sinal,
                        macd.Histograma);


                // ==========================================
                // CONFIRMAÇÃO DAS MÉDIAS
                // ==========================================

                var mediasConfirmamCompra =
                    media10_5M > 0 &&
                    media50_5M > 0 &&
                    media200_5M > 0 &&
                    media10_5M > media50_5M &&
                    media50_5M > media200_5M &&
                    precoAtual > media10_5M;

                var mediasConfirmamVenda =
                    media10_5M > 0 &&
                    media50_5M > 0 &&
                    media200_5M > 0 &&
                    media10_5M < media50_5M &&
                    media50_5M < media200_5M &&
                    precoAtual < media10_5M;


                // ==========================================
                // CONFIRMAÇÃO BOLLINGER
                // ==========================================

                var bollingerConfirmaCompra =
                    situacaoBollinger == "Compra";

                var bollingerConfirmaVenda =
                    situacaoBollinger == "Venda";


                // ==========================================
                // CONFIRMAÇÃO RSI
                // ==========================================

                var rsiConfirmaCompra =
                    situacaoRsi == "Compra";

                var rsiConfirmaVenda =
                    situacaoRsi == "Venda";


                // ==========================================
                // CONFIRMAÇÃO MACD
                // ==========================================

                var macdConfirmaCompra =
                    situacaoMacd == "Compra";

                var macdConfirmaVenda =
                    situacaoMacd == "Venda";


                // ==========================================
                // RESULTADO
                // ==========================================

                var resultado =
                    new ResultadoAnalise
                    {
                        Simbolo = simbolo,

                        PrecoAtual = precoAtual,

                        DataHora =
                            velas5M[^1].DataHora,

                        Tendencia2H =
                            tendencia2H,

                        Tendencia1H =
                            tendencia1H,

                        Estrutura30M =
                            estrutura30M,

                        Pullback15M =
                            pullback15M,

                        Confirmacao5M =
                            confirmacao5M,


                        // EMAs antigas
                        Ema10_2H = ema10_2H,
                        Ema20_2H = ema20_2H,

                        Ema10_1H = ema10_1H,
                        Ema20_1H = ema20_1H,

                        Ema10_30M = ema10_30M,
                        Ema20_30M = ema20_30M,

                        Ema10_15M = ema10_15M,
                        Ema20_15M = ema20_15M,

                        Ema10_5M = ema10_5M,
                        Ema20_5M = ema20_5M,


                        // Médias novas
                        Media10_5M = media10_5M,
                        Media50_5M = media50_5M,
                        Media200_5M = media200_5M,

                        Media10_15M = media10_15M,
                        Media50_15M = media50_15M,
                        Media200_15M = media200_15M,

                        Media10_30M = media10_30M,
                        Media50_30M = media50_30M,
                        Media200_30M = media200_30M,

                        Media10_1H = media10_1H,
                        Media50_1H = media50_1H,
                        Media200_1H = media200_1H,

                        Media10_2H = media10_2H,
                        Media50_2H = media50_2H,
                        Media200_2H = media200_2H,


                        // Bollinger
                        BollingerSuperior5M =
                            bollinger.Superior,

                        BollingerMedia5M =
                            bollinger.Media,

                        BollingerInferior5M =
                            bollinger.Inferior,

                        SituacaoBollinger5M =
                            situacaoBollinger,


                        // RSI
                        Rsi14_5M =
                            rsi5M,

                        Rsi14_15M =
                            rsi15M,

                        SituacaoRsi =
                            situacaoRsi,


                        // MACD
                        Macd5M =
                            macd.Macd,

                        MacdSinal5M =
                            macd.Sinal,

                        MacdHistograma5M =
                            macd.Histograma,

                        SituacaoMacd =
                            situacaoMacd,


                        // Suporte e resistência
                        Suporte =
                            suporte,

                        Resistencia =
                            resistencia,


                        // Cenário
                        TendenciasAlinhadas =
                            tendenciasAlinhadas,

                        ProximoSuporte =
                            proximoSuporte,

                        ProximoResistencia =
                            proximoResistencia,

                        PullbackConfirmado =
                            pullbackConfirmado,

                        GatilhoConfirmado =
                            gatilhoConfirmado,


                        // Confirmações novas
                        MediasConfirmamCompra =
                            mediasConfirmamCompra,

                        MediasConfirmamVenda =
                            mediasConfirmamVenda,

                        BollingerConfirmaCompra =
                            bollingerConfirmaCompra,

                        BollingerConfirmaVenda =
                            bollingerConfirmaVenda,

                        RsiConfirmaCompra =
                            rsiConfirmaCompra,

                        RsiConfirmaVenda =
                            rsiConfirmaVenda,

                        MacdConfirmaCompra =
                            macdConfirmaCompra,

                        MacdConfirmaVenda =
                            macdConfirmaVenda
                    };


                CalcularResultado(
                    resultado,
                    pontuacaoSinalModerado,
                    pontuacaoSinalForte);

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao analisar o ativo {Simbolo}.",
                    simbolo);

                throw;
            }
        }


        // ==========================================
        // CONFIGURAÇÕES
        // ==========================================

        private int ObterIntConfiguracao(
            string chave,
            int valorPadrao)
        {
            var valor =
                _configuration[chave];

            if (int.TryParse(
                    valor,
                    out var resultado) &&
                resultado > 0)
            {
                return resultado;
            }

            return valorPadrao;
        }


        private decimal ObterDecimalConfiguracao(
            string chave,
            decimal valorPadrao)
        {
            var valor =
                _configuration[chave];

            if (string.IsNullOrWhiteSpace(valor))
                return valorPadrao;

            valor =
                valor.Replace(
                    ",",
                    ".",
                    StringComparison.Ordinal);

            if (decimal.TryParse(
                    valor,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var resultado) &&
                resultado > 0)
            {
                return resultado;
            }

            return valorPadrao;
        }


        // ==========================================
        // EMA
        // ==========================================

        private static decimal CalcularEma(
            List<VelaMercado> velas,
            int periodo)
        {
            if (periodo <= 0 ||
                velas.Count < periodo)
            {
                return 0;
            }

            var fechamentos =
                velas
                    .Select(v => v.Fechamento)
                    .ToList();

            return CalcularEmaValores(
                fechamentos,
                periodo);
        }


        private static decimal CalcularEmaValores(
            List<decimal> valores,
            int periodo)
        {
            if (periodo <= 0 ||
                valores.Count < periodo)
            {
                return 0;
            }

            var multiplicador =
                2m / (periodo + 1);

            var ema =
                valores
                    .Take(periodo)
                    .Average();

            for (var i = periodo;
                 i < valores.Count;
                 i++)
            {
                ema =
                    ((valores[i] - ema) *
                     multiplicador)
                    + ema;
            }

            return ema;
        }


        private static List<decimal>
            CalcularSerieEma(
                List<decimal> valores,
                int periodo)
        {
            var resultado =
                new List<decimal>();

            if (periodo <= 0 ||
                valores.Count < periodo)
            {
                return resultado;
            }

            var multiplicador =
                2m / (periodo + 1);

            var ema =
                valores
                    .Take(periodo)
                    .Average();

            resultado.Add(ema);

            for (var i = periodo;
                 i < valores.Count;
                 i++)
            {
                ema =
                    ((valores[i] - ema) *
                     multiplicador)
                    + ema;

                resultado.Add(ema);
            }

            return resultado;
        }


        // ==========================================
        // TENDÊNCIA
        // ==========================================

        private static string AnalisarTendencia(
            List<VelaMercado> velas,
            decimal ema10,
            decimal ema20)
        {
            if (velas.Count == 0 ||
                ema10 <= 0 ||
                ema20 <= 0)
            {
                return "Neutra";
            }

            var ultima =
                velas[^1];

            if (ema10 > ema20 &&
                ultima.Fechamento > ema10)
            {
                return "Alta";
            }

            if (ema10 < ema20 &&
                ultima.Fechamento < ema10)
            {
                return "Baixa";
            }

            return "Neutra";
        }


        // ==========================================
        // PULLBACK
        // ==========================================

        private static string AnalisarPullback(
            List<VelaMercado> velas,
            string tendencia,
            decimal ema10,
            decimal ema20)
        {
            if (velas.Count < 2 ||
                ema10 <= 0 ||
                ema20 <= 0)
            {
                return "Não confirmado";
            }

            var ultima =
                velas[^1];

            var margem =
                ultima.Fechamento *
                0.0025m;

            if (tendencia == "Alta")
            {
                var tocouEma =
                    ultima.Minima <=
                        ema10 + margem &&
                    ultima.Maxima >=
                        ema20 - margem;

                var rejeitouParaCima =
                    ultima.Fechamento >
                        ultima.Abertura &&
                    ultima.Fechamento >
                        ema10;

                if (tocouEma &&
                    rejeitouParaCima)
                {
                    return
                        "Confirmado para compra";
                }
            }

            if (tendencia == "Baixa")
            {
                var tocouEma =
                    ultima.Maxima >=
                        ema10 - margem &&
                    ultima.Minima <=
                        ema20 + margem;

                var rejeitouParaBaixo =
                    ultima.Fechamento <
                        ultima.Abertura &&
                    ultima.Fechamento <
                        ema10;

                if (tocouEma &&
                    rejeitouParaBaixo)
                {
                    return
                        "Confirmado para venda";
                }
            }

            return "Não confirmado";
        }


        // ==========================================
        // CONFIRMAÇÃO 5M
        // ==========================================

        private static string AnalisarConfirmacao(
            List<VelaMercado> velas,
            string tendencia,
            decimal ema10,
            decimal ema20)
        {
            if (velas.Count < 3 ||
                ema10 <= 0 ||
                ema20 <= 0)
            {
                return "Aguardando";
            }

            var ultima =
                velas[^1];

            var anterior =
                velas[^2];

            if (tendencia == "Alta" &&
                ema10 > ema20 &&
                ultima.Fechamento >
                    ultima.Abertura &&
                ultima.Fechamento >
                    ema10 &&
                ultima.Fechamento >
                    anterior.Maxima)
            {
                return "Compra";
            }

            if (tendencia == "Baixa" &&
                ema10 < ema20 &&
                ultima.Fechamento <
                    ultima.Abertura &&
                ultima.Fechamento <
                    ema10 &&
                ultima.Fechamento <
                    anterior.Minima)
            {
                return "Venda";
            }

            return "Aguardando";
        }


        // ==========================================
        // BOLLINGER
        // ==========================================

        private static (
            decimal Superior,
            decimal Media,
            decimal Inferior)
            CalcularBollinger(
                List<VelaMercado> velas,
                int periodo,
                decimal multiplicadorDesvio)
        {
            if (periodo <= 0 ||
                velas.Count < periodo)
            {
                return (0, 0, 0);
            }

            var fechamentos =
                velas
                    .TakeLast(periodo)
                    .Select(v => v.Fechamento)
                    .ToList();

            var media =
                fechamentos.Average();

            var variancia =
                fechamentos
                    .Select(
                        valor =>
                            (valor - media) *
                            (valor - media))
                    .Average();

            var desvio =
                (decimal)Math.Sqrt(
                    (double)variancia);

            var superior =
                media +
                (desvio *
                 multiplicadorDesvio);

            var inferior =
                media -
                (desvio *
                 multiplicadorDesvio);

            return (
                superior,
                media,
                inferior);
        }


        private static string AnalisarBollinger(
            List<VelaMercado> velas,
            decimal superior,
            decimal media,
            decimal inferior)
        {
            if (velas.Count < 2 ||
                superior <= 0 ||
                media <= 0 ||
                inferior <= 0)
            {
                return "Neutra";
            }

            var ultima =
                velas[^1];

            var anterior =
                velas[^2];

            // Reação na região inferior e recuperação.
            if (ultima.Minima <= inferior &&
                ultima.Fechamento >
                    ultima.Abertura &&
                ultima.Fechamento >
                    inferior)
            {
                return "Compra";
            }

            // Reação na região superior e rejeição.
            if (ultima.Maxima >= superior &&
                ultima.Fechamento <
                    ultima.Abertura &&
                ultima.Fechamento <
                    superior)
            {
                return "Venda";
            }

            // Continuação acima da média.
            if (ultima.Fechamento > media &&
                anterior.Fechamento <= media &&
                ultima.Fechamento >
                    ultima.Abertura)
            {
                return "Compra";
            }

            // Continuação abaixo da média.
            if (ultima.Fechamento < media &&
                anterior.Fechamento >= media &&
                ultima.Fechamento <
                    ultima.Abertura)
            {
                return "Venda";
            }

            return "Neutra";
        }


        // ==========================================
        // RSI
        // ==========================================

        private static decimal CalcularRsi(
            List<VelaMercado> velas,
            int periodo)
        {
            if (periodo <= 0 ||
                velas.Count < periodo + 1)
            {
                return 50m;
            }

            var fechamentos =
                velas
                    .Select(v => v.Fechamento)
                    .ToList();

            decimal ganhos = 0;
            decimal perdas = 0;

            var inicio =
                fechamentos.Count -
                periodo;

            for (var i = inicio;
                 i < fechamentos.Count;
                 i++)
            {
                var diferenca =
                    fechamentos[i] -
                    fechamentos[i - 1];

                if (diferenca > 0)
                {
                    ganhos += diferenca;
                }
                else if (diferenca < 0)
                {
                    perdas +=
                        Math.Abs(diferenca);
                }
            }

            var mediaGanhos =
                ganhos / periodo;

            var mediaPerdas =
                perdas / periodo;

            if (mediaPerdas == 0 &&
                mediaGanhos == 0)
            {
                return 50m;
            }

            if (mediaPerdas == 0)
                return 100m;

            if (mediaGanhos == 0)
                return 0m;

            var rs =
                mediaGanhos /
                mediaPerdas;

            return
                100m -
                (100m /
                 (1m + rs));
        }


        private static string AnalisarRsi(
            decimal rsi5M,
            decimal rsi15M,
            decimal sobrevendido,
            decimal sobrecomprado)
        {
            // Evita comprar quando o mercado já
            // está extremamente esticado para cima
            // e evita vender quando está extremamente
            // esticado para baixo.

            var centro =
                50m;

            if (rsi5M > centro &&
                rsi5M < sobrecomprado &&
                rsi15M >= centro)
            {
                return "Compra";
            }

            if (rsi5M < centro &&
                rsi5M > sobrevendido &&
                rsi15M <= centro)
            {
                return "Venda";
            }

            return "Neutra";
        }


        // ==========================================
        // MACD
        // ==========================================

        private static (
            decimal Macd,
            decimal Sinal,
            decimal Histograma)
            CalcularMacd(
                List<VelaMercado> velas,
                int periodoRapido,
                int periodoLento,
                int periodoSinal)
        {
            if (periodoRapido <= 0 ||
                periodoLento <= 0 ||
                periodoSinal <= 0 ||
                periodoRapido >= periodoLento ||
                velas.Count <
                    periodoLento +
                    periodoSinal)
            {
                return (0, 0, 0);
            }

            var fechamentos =
                velas
                    .Select(v => v.Fechamento)
                    .ToList();

            var serieRapida =
                CalcularSerieEma(
                    fechamentos,
                    periodoRapido);

            var serieLenta =
                CalcularSerieEma(
                    fechamentos,
                    periodoLento);

            if (serieRapida.Count == 0 ||
                serieLenta.Count == 0)
            {
                return (0, 0, 0);
            }

            var deslocamento =
                periodoLento -
                periodoRapido;

            var linhaMacd =
                new List<decimal>();

            for (var i = 0;
                 i < serieLenta.Count;
                 i++)
            {
                var indiceRapido =
                    i + deslocamento;

                if (indiceRapido >=
                    serieRapida.Count)
                {
                    break;
                }

                linhaMacd.Add(
                    serieRapida[indiceRapido] -
                    serieLenta[i]);
            }

            if (linhaMacd.Count <
                periodoSinal)
            {
                return (0, 0, 0);
            }

            var linhaSinal =
                CalcularSerieEma(
                    linhaMacd,
                    periodoSinal);

            if (linhaSinal.Count == 0)
            {
                return (0, 0, 0);
            }

            var macdAtual =
                linhaMacd[^1];

            var sinalAtual =
                linhaSinal[^1];

            var histograma =
                macdAtual -
                sinalAtual;

            return (
                macdAtual,
                sinalAtual,
                histograma);
        }


        private static string AnalisarMacd(
            decimal macd,
            decimal sinal,
            decimal histograma)
        {
            if (macd > sinal &&
                histograma > 0)
            {
                return "Compra";
            }

            if (macd < sinal &&
                histograma < 0)
            {
                return "Venda";
            }

            return "Neutra";
        }


        // ==========================================
        // SUPORTE E RESISTÊNCIA
        // ==========================================

        private static decimal CalcularSuporte(
            List<VelaMercado> velas,
            int periodo)
        {
            if (velas.Count == 0)
                return 0;

            periodo =
                Math.Min(
                    periodo,
                    velas.Count);

            return velas
                .TakeLast(periodo)
                .Min(v => v.Minima);
        }


        private static decimal CalcularResistencia(
            List<VelaMercado> velas,
            int periodo)
        {
            if (velas.Count == 0)
                return 0;

            periodo =
                Math.Min(
                    periodo,
                    velas.Count);

            return velas
                .TakeLast(periodo)
                .Max(v => v.Maxima);
        }


        private static bool EstaProximo(
            decimal preco,
            decimal nivel,
            decimal percentual)
        {
            if (preco <= 0 ||
                nivel <= 0)
            {
                return false;
            }

            var distancia =
                Math.Abs(
                    preco - nivel) /
                preco;

            return
                distancia <= percentual;
        }


        // ==========================================
        // PONTUAÇÃO FINAL
        // ==========================================

        private static void CalcularResultado(
            ResultadoAnalise resultado,
            int pontuacaoModerado,
            int pontuacaoForte)
        {
            var pontosCompra = 0;
            var pontosVenda = 0;


            // Tendência 2H
            if (resultado.Tendencia2H ==
                "Alta")
            {
                pontosCompra += 15;
            }

            if (resultado.Tendencia2H ==
                "Baixa")
            {
                pontosVenda += 15;
            }


            // Tendência 1H
            if (resultado.Tendencia1H ==
                "Alta")
            {
                pontosCompra += 15;
            }

            if (resultado.Tendencia1H ==
                "Baixa")
            {
                pontosVenda += 15;
            }


            // Estrutura 30M
            if (resultado.Estrutura30M ==
                "Alta")
            {
                pontosCompra += 10;
            }

            if (resultado.Estrutura30M ==
                "Baixa")
            {
                pontosVenda += 10;
            }


            // Alinhamento completo
            if (resultado.TendenciasAlinhadas)
            {
                if (resultado.Tendencia1H ==
                    "Alta")
                {
                    pontosCompra += 10;
                }

                if (resultado.Tendencia1H ==
                    "Baixa")
                {
                    pontosVenda += 10;
                }
            }


            // Pullback 15M
            if (resultado.Pullback15M ==
                "Confirmado para compra")
            {
                pontosCompra += 15;
            }

            if (resultado.Pullback15M ==
                "Confirmado para venda")
            {
                pontosVenda += 15;
            }


            // Gatilho 5M
            if (resultado.Confirmacao5M ==
                "Compra")
            {
                pontosCompra += 15;
            }

            if (resultado.Confirmacao5M ==
                "Venda")
            {
                pontosVenda += 15;
            }


            // Médias 10 / 50 / 200
            if (resultado.MediasConfirmamCompra)
            {
                pontosCompra += 5;
            }

            if (resultado.MediasConfirmamVenda)
            {
                pontosVenda += 5;
            }


            // Bollinger
            if (resultado.BollingerConfirmaCompra)
            {
                pontosCompra += 5;
            }

            if (resultado.BollingerConfirmaVenda)
            {
                pontosVenda += 5;
            }


            // RSI
            if (resultado.RsiConfirmaCompra)
            {
                pontosCompra += 5;
            }

            if (resultado.RsiConfirmaVenda)
            {
                pontosVenda += 5;
            }


            // MACD
            if (resultado.MacdConfirmaCompra)
            {
                pontosCompra += 5;
            }

            if (resultado.MacdConfirmaVenda)
            {
                pontosVenda += 5;
            }


            // Suporte
            if (resultado.ProximoSuporte &&
                resultado.Tendencia1H ==
                    "Alta")
            {
                pontosCompra += 5;
            }


            // Resistência
            if (resultado.ProximoResistencia &&
                resultado.Tendencia1H ==
                    "Baixa")
            {
                pontosVenda += 5;
            }


            pontosCompra =
                Math.Min(
                    pontosCompra,
                    100);

            pontosVenda =
                Math.Min(
                    pontosVenda,
                    100);


            if (pontosCompra >
                pontosVenda)
            {
                resultado.Pontuacao =
                    pontosCompra;
            }
            else if (pontosVenda >
                     pontosCompra)
            {
                resultado.Pontuacao =
                    pontosVenda;
            }
            else
            {
                resultado.Pontuacao = 0;
            }


            // ==========================================
            // ENTRADA REAL
            //
            // Os indicadores aumentam a qualidade,
            // mas NÃO substituem pullback + gatilho.
            // ==========================================

            var compraConfirmada =
                pontosCompra >
                    pontosVenda &&
                resultado.Pullback15M ==
                    "Confirmado para compra" &&
                resultado.Confirmacao5M ==
                    "Compra";

            var vendaConfirmada =
                pontosVenda >
                    pontosCompra &&
                resultado.Pullback15M ==
                    "Confirmado para venda" &&
                resultado.Confirmacao5M ==
                    "Venda";


            if (compraConfirmada)
            {
                resultado.Direcao =
                    "COMPRAR";
            }
            else if (vendaConfirmada)
            {
                resultado.Direcao =
                    "VENDER";
            }
            else
            {
                resultado.Direcao =
                    "AGUARDAR";
            }


            if (resultado.Direcao !=
                    "AGUARDAR" &&
                resultado.Pontuacao >=
                    pontuacaoForte)
            {
                resultado.Forca =
                    "FORTE";
            }
            else if (
                resultado.Direcao !=
                    "AGUARDAR" &&
                resultado.Pontuacao >=
                    pontuacaoModerado)
            {
                resultado.Forca =
                    "MODERADO";
            }
            else
            {
                resultado.Forca =
                    "SEM SINAL";

                resultado.Direcao =
                    "AGUARDAR";
            }


            // ==========================================
            // MOTIVOS
            // ==========================================

            resultado.Motivos.Clear();

            resultado.Motivos.Add(
                $"Tendência 2H: {resultado.Tendencia2H}");

            resultado.Motivos.Add(
                $"Tendência 1H: {resultado.Tendencia1H}");

            resultado.Motivos.Add(
                $"Estrutura 30M: {resultado.Estrutura30M}");

            resultado.Motivos.Add(
                $"Pullback 15M: {resultado.Pullback15M}");

            resultado.Motivos.Add(
                $"Confirmação 5M: {resultado.Confirmacao5M}");

            resultado.Motivos.Add(
                $"RSI 5M: {resultado.Rsi14_5M:F2}");

            resultado.Motivos.Add(
                $"RSI 15M: {resultado.Rsi14_15M:F2}");

            resultado.Motivos.Add(
                $"RSI: {resultado.SituacaoRsi}");

            resultado.Motivos.Add(
                $"MACD: {resultado.SituacaoMacd}");

            resultado.Motivos.Add(
                $"Bollinger 5M: {resultado.SituacaoBollinger5M}");

            if (resultado.MediasConfirmamCompra)
            {
                resultado.Motivos.Add(
                    "Médias 10/50/200 alinhadas para compra.");
            }

            if (resultado.MediasConfirmamVenda)
            {
                resultado.Motivos.Add(
                    "Médias 10/50/200 alinhadas para venda.");
            }

            resultado.Motivos.Add(
                resultado.TendenciasAlinhadas
                    ? "Tendências principais alinhadas."
                    : "Tendências principais não estão totalmente alinhadas.");

            if (resultado.ProximoSuporte)
            {
                resultado.Motivos.Add(
                    "Preço próximo da região de suporte.");
            }

            if (resultado.ProximoResistencia)
            {
                resultado.Motivos.Add(
                    "Preço próximo da região de resistência.");
            }
        }


        // ==========================================
        // AGRUPAMENTO DOS TIMEFRAMES
        // ==========================================

        private static List<VelaMercado>
            AgruparVelasPorIntervalo(
                List<VelaMercado> velas5M,
                int intervaloMinutos)
        {
            if (intervaloMinutos <= 0 ||
                intervaloMinutos % 5 != 0)
            {
                throw new ArgumentException(
                    "O intervalo deve ser maior que zero e múltiplo de 5 minutos.",
                    nameof(intervaloMinutos));
            }

            var quantidadeEsperada =
                intervaloMinutos / 5;

            var ordenadas =
                velas5M
                    .OrderBy(v => v.DataHora)
                    .ToList();

            var grupos =
                ordenadas
                    .GroupBy(v =>
                    {
                        var minutosDoDia =
                            (v.DataHora.Hour * 60) +
                            v.DataHora.Minute;

                        var inicioDoBloco =
                            (minutosDoDia /
                             intervaloMinutos) *
                            intervaloMinutos;

                        return
                            v.DataHora.Date
                                .AddMinutes(
                                    inicioDoBloco);
                    });

            var resultado =
                new List<VelaMercado>();

            foreach (var grupoOriginal in grupos)
            {
                var grupo =
                    grupoOriginal
                        .OrderBy(v => v.DataHora)
                        .ToList();

                // Ignora blocos incompletos.
                if (grupo.Count !=
                    quantidadeEsperada)
                {
                    continue;
                }

                var sequenciaCompleta =
                    true;

                for (var i = 1;
                     i < grupo.Count;
                     i++)
                {
                    var diferenca =
                        grupo[i].DataHora -
                        grupo[i - 1].DataHora;

                    if (diferenca !=
                        TimeSpan.FromMinutes(5))
                    {
                        sequenciaCompleta =
                            false;

                        break;
                    }
                }

                if (!sequenciaCompleta)
                    continue;

                resultado.Add(
                    new VelaMercado
                    {
                        DataHora =
                            grupoOriginal.Key,

                        Abertura =
                            grupo.First().Abertura,

                        Maxima =
                            grupo.Max(
                                v => v.Maxima),

                        Minima =
                            grupo.Min(
                                v => v.Minima),

                        Fechamento =
                            grupo.Last().Fechamento,

                        Volume =
                            grupo.Sum(
                                v => v.Volume)
                    });
            }

            return resultado
                .OrderBy(v => v.DataHora)
                .ToList();
        }
    }
}