using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public class AnaliseMercadoService : IAnaliseMercadoService
    {
        private readonly ITwelveDataService _twelveDataService;
        private readonly ILogger<AnaliseMercadoService> _logger;

        public AnaliseMercadoService(
            ITwelveDataService twelveDataService,
            ILogger<AnaliseMercadoService> logger)
        {
            _twelveDataService = twelveDataService;
            _logger = logger;
        }

        public async Task<ResultadoAnalise> AnalisarAsync(string simbolo)
        {
            try
            {
                // Faz apenas UMA consulta à API.
                // Busca velas de 5 minutos e monta internamente
                // os timeframes de 15M, 30M, 1H e 2H.
                var velas5M = await _twelveDataService.ObterVelasAsync(
                    simbolo,
                    "5min",
                    1500);

                if (velas5M.Count < 480)
                {
                    throw new InvalidOperationException(
                        "Quantidade de velas de 5 minutos insuficiente para realizar a análise.");
                }

                var velas15M = AgruparVelasPorIntervalo(
                    velas5M,
                    15);

                var velas30M = AgruparVelasPorIntervalo(
                    velas5M,
                    30);

                var velas1H = AgruparVelasPorIntervalo(
                    velas5M,
                    60);

                var velas2H = AgruparVelasPorIntervalo(
                    velas5M,
                    120);

                if (velas15M.Count < 50 ||
                    velas30M.Count < 50 ||
                    velas1H.Count < 50 ||
                    velas2H.Count < 20)
                {
                    throw new InvalidOperationException(
                        "Quantidade de velas agrupadas insuficiente para realizar a análise.");
                }

                // EMA 10 e EMA 20
                var ema10_2H = CalcularEma(velas2H, 10);
                var ema20_2H = CalcularEma(velas2H, 20);

                var ema10_1H = CalcularEma(velas1H, 10);
                var ema20_1H = CalcularEma(velas1H, 20);

                var ema10_30M = CalcularEma(velas30M, 10);
                var ema20_30M = CalcularEma(velas30M, 20);

                var ema10_15M = CalcularEma(velas15M, 10);
                var ema20_15M = CalcularEma(velas15M, 20);

                var ema10_5M = CalcularEma(velas5M, 10);
                var ema20_5M = CalcularEma(velas5M, 20);

                // Tendências
                var tendencia2H = AnalisarTendencia(
                    velas2H,
                    ema10_2H,
                    ema20_2H);

                var tendencia1H = AnalisarTendencia(
                    velas1H,
                    ema10_1H,
                    ema20_1H);

                var estrutura30M = AnalisarTendencia(
                    velas30M,
                    ema10_30M,
                    ema20_30M);

                // Suporte e resistência
                var suporte = CalcularSuporte(
                    velas30M,
                    20);

                var resistencia = CalcularResistencia(
                    velas30M,
                    20);

                var precoAtual =
                    velas5M[^1].Fechamento;

                var proximoSuporte = EstaProximo(
                    precoAtual,
                    suporte,
                    0.005m);

                var proximoResistencia = EstaProximo(
                    precoAtual,
                    resistencia,
                    0.005m);

                // Alinhamento
                var tendenciasAlinhadas =
                    (tendencia2H == "Alta" &&
                     tendencia1H == "Alta" &&
                     estrutura30M == "Alta")
                    ||
                    (tendencia2H == "Baixa" &&
                     tendencia1H == "Baixa" &&
                     estrutura30M == "Baixa");

                // Pullback
                var pullback15M = AnalisarPullback(
                    velas15M,
                    tendencia1H,
                    ema10_15M,
                    ema20_15M);

                var pullbackConfirmado =
                    pullback15M ==
                        "Confirmado para compra" ||
                    pullback15M ==
                        "Confirmado para venda";

                // Gatilho 5M
                var confirmacao5M = AnalisarConfirmacao(
                    velas5M,
                    tendencia1H,
                    ema10_5M,
                    ema20_5M);

                var gatilhoConfirmado =
                    confirmacao5M == "Compra" ||
                    confirmacao5M == "Venda";

                var resultado = new ResultadoAnalise
                {
                    Simbolo = simbolo,

                    PrecoAtual = precoAtual,

                    DataHora = velas5M[^1].DataHora,

                    Tendencia2H = tendencia2H,

                    Tendencia1H = tendencia1H,

                    Estrutura30M = estrutura30M,

                    Pullback15M = pullback15M,

                    Confirmacao5M = confirmacao5M,

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

                    Suporte = suporte,

                    Resistencia = resistencia,

                    TendenciasAlinhadas =
                        tendenciasAlinhadas,

                    ProximoSuporte =
                        proximoSuporte,

                    ProximoResistencia =
                        proximoResistencia,

                    PullbackConfirmado =
                        pullbackConfirmado,

                    GatilhoConfirmado =
                        gatilhoConfirmado
                };

                CalcularResultado(resultado);

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

        private static decimal CalcularEma(
            List<VelaMercado> velas,
            int periodo)
        {
            if (velas.Count < periodo)
                return 0;

            var fechamentos = velas
                .Select(v => v.Fechamento)
                .ToList();

            var multiplicador =
                2m / (periodo + 1);

            var ema = fechamentos
                .Take(periodo)
                .Average();

            for (var i = periodo;
                 i < fechamentos.Count;
                 i++)
            {
                ema =
                    ((fechamentos[i] - ema) *
                     multiplicador)
                    + ema;
            }

            return ema;
        }

        private static string AnalisarTendencia(
            List<VelaMercado> velas,
            decimal ema10,
            decimal ema20)
        {
            var ultima = velas[^1];

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

        private static string AnalisarPullback(
            List<VelaMercado> velas,
            string tendencia,
            decimal ema10,
            decimal ema20)
        {
            if (velas.Count < 2)
                return "Não confirmado";

            var ultima = velas[^1];

            var margem =
                ultima.Fechamento * 0.0025m;

            if (tendencia == "Alta")
            {
                var tocouEma =
                    ultima.Minima <= ema10 + margem &&
                    ultima.Maxima >= ema20 - margem;

                var rejeitouParaCima =
                    ultima.Fechamento >
                        ultima.Abertura &&
                    ultima.Fechamento > ema10;

                if (tocouEma &&
                    rejeitouParaCima)
                {
                    return "Confirmado para compra";
                }
            }

            if (tendencia == "Baixa")
            {
                var tocouEma =
                    ultima.Maxima >= ema10 - margem &&
                    ultima.Minima <= ema20 + margem;

                var rejeitouParaBaixo =
                    ultima.Fechamento <
                        ultima.Abertura &&
                    ultima.Fechamento < ema10;

                if (tocouEma &&
                    rejeitouParaBaixo)
                {
                    return "Confirmado para venda";
                }
            }

            return "Não confirmado";
        }

        private static string AnalisarConfirmacao(
            List<VelaMercado> velas,
            string tendencia,
            decimal ema10,
            decimal ema20)
        {
            if (velas.Count < 3)
                return "Aguardando";

            var ultima = velas[^1];
            var anterior = velas[^2];

            if (tendencia == "Alta" &&
                ema10 > ema20 &&
                ultima.Fechamento > ultima.Abertura &&
                ultima.Fechamento > ema10 &&
                ultima.Fechamento > anterior.Maxima)
            {
                return "Compra";
            }

            if (tendencia == "Baixa" &&
                ema10 < ema20 &&
                ultima.Fechamento < ultima.Abertura &&
                ultima.Fechamento < ema10 &&
                ultima.Fechamento < anterior.Minima)
            {
                return "Venda";
            }

            return "Aguardando";
        }

        private static decimal CalcularSuporte(
            List<VelaMercado> velas,
            int periodo)
        {
            return velas
                .TakeLast(periodo)
                .Min(v => v.Minima);
        }

        private static decimal CalcularResistencia(
            List<VelaMercado> velas,
            int periodo)
        {
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
                Math.Abs(preco - nivel) /
                preco;

            return distancia <= percentual;
        }

        private static void CalcularResultado(
            ResultadoAnalise resultado)
        {
            var pontosCompra = 0;
            var pontosVenda = 0;

            // Tendência 2H
            if (resultado.Tendencia2H == "Alta")
                pontosCompra += 20;

            if (resultado.Tendencia2H == "Baixa")
                pontosVenda += 20;

            // Tendência 1H
            if (resultado.Tendencia1H == "Alta")
                pontosCompra += 20;

            if (resultado.Tendencia1H == "Baixa")
                pontosVenda += 20;

            // Estrutura 30M
            if (resultado.Estrutura30M == "Alta")
                pontosCompra += 15;

            if (resultado.Estrutura30M == "Baixa")
                pontosVenda += 15;

            // Bônus por alinhamento completo
            if (resultado.TendenciasAlinhadas)
            {
                if (resultado.Tendencia1H == "Alta")
                    pontosCompra += 10;

                if (resultado.Tendencia1H == "Baixa")
                    pontosVenda += 10;
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
            if (resultado.Confirmacao5M == "Compra")
                pontosCompra += 15;

            if (resultado.Confirmacao5M == "Venda")
                pontosVenda += 15;

            // Região de suporte
            if (resultado.ProximoSuporte &&
                resultado.Tendencia1H == "Alta")
            {
                pontosCompra += 5;
            }

            // Região de resistência
            if (resultado.ProximoResistencia &&
                resultado.Tendencia1H == "Baixa")
            {
                pontosVenda += 5;
            }

            pontosCompra =
                Math.Min(pontosCompra, 100);

            pontosVenda =
                Math.Min(pontosVenda, 100);

            if (pontosCompra > pontosVenda)
            {
                resultado.Pontuacao =
                    pontosCompra;
            }
            else if (pontosVenda > pontosCompra)
            {
                resultado.Pontuacao =
                    pontosVenda;
            }
            else
            {
                resultado.Pontuacao = 0;
            }

            /*
             * Para existir uma entrada real:
             * - cenário precisa ter direção;
             * - pullback precisa estar confirmado;
             * - gatilho 5M precisa estar confirmado;
             * - gatilho deve concordar com a direção.
             */

            var compraConfirmada =
                pontosCompra > pontosVenda &&
                resultado.Pullback15M ==
                    "Confirmado para compra" &&
                resultado.Confirmacao5M ==
                    "Compra";

            var vendaConfirmada =
                pontosVenda > pontosCompra &&
                resultado.Pullback15M ==
                    "Confirmado para venda" &&
                resultado.Confirmacao5M ==
                    "Venda";

            if (compraConfirmada)
            {
                resultado.Direcao = "COMPRAR";
            }
            else if (vendaConfirmada)
            {
                resultado.Direcao = "VENDER";
            }
            else
            {
                resultado.Direcao = "AGUARDAR";
            }

            if (resultado.Direcao != "AGUARDAR" &&
                resultado.Pontuacao >= 80)
            {
                resultado.Forca = "FORTE";
            }
            else if (
                resultado.Direcao != "AGUARDAR" &&
                resultado.Pontuacao >= 60)
            {
                resultado.Forca = "MODERADO";
            }
            else
            {
                resultado.Forca = "SEM SINAL";
                resultado.Direcao = "AGUARDAR";
            }

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

            var ordenadas = velas5M
                .OrderBy(v => v.DataHora)
                .ToList();

            var grupos = ordenadas
                .GroupBy(v =>
                {
                    var minutosDoDia =
                        (v.DataHora.Hour * 60) +
                        v.DataHora.Minute;

                    var inicioDoBloco =
                        (minutosDoDia /
                         intervaloMinutos) *
                        intervaloMinutos;

                    return v.DataHora.Date
                        .AddMinutes(inicioDoBloco);
                });

            var resultado =
                new List<VelaMercado>();

            foreach (var grupoOriginal in grupos)
            {
                var grupo = grupoOriginal
                    .OrderBy(v => v.DataHora)
                    .ToList();

                // Ignora blocos incompletos.
                // Isso evita criar velas artificiais
                // quando houver lacunas no mercado.
                if (grupo.Count != quantidadeEsperada)
                {
                    continue;
                }

                var sequenciaCompleta = true;

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
                        sequenciaCompleta = false;
                        break;
                    }
                }

                if (!sequenciaCompleta)
                {
                    continue;
                }

                resultado.Add(
                    new VelaMercado
                    {
                        DataHora =
                            grupoOriginal.Key,

                        Abertura =
                            grupo.First().Abertura,

                        Maxima =
                            grupo.Max(v => v.Maxima),

                        Minima =
                            grupo.Min(v => v.Minima),

                        Fechamento =
                            grupo.Last().Fechamento,

                        Volume =
                            grupo.Sum(v => v.Volume)
                    });
            }

            return resultado
                .OrderBy(v => v.DataHora)
                .ToList();
        }
    }
}