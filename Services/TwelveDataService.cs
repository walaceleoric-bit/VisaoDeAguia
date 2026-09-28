using System.Globalization;
using System.Text.Json;
using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public class TwelveDataService : ITwelveDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwelveDataService> _logger;

        public TwelveDataService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TwelveDataService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<VelaMercado>> ObterVelasAsync(
            string simbolo,
            string intervalo,
            int quantidade = 100)
        {
            var apiKey =
                _configuration["RapidApi:TwelveData:ApiKey"];

            var apiHost =
                _configuration["RapidApi:TwelveData:Host"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "A chave da RapidAPI não foi configurada.");
            }

            if (string.IsNullOrWhiteSpace(apiHost))
            {
                throw new InvalidOperationException(
                    "O host da Twelve Data não foi configurado.");
            }

            var client =
                _httpClientFactory.CreateClient("TwelveData");

            // Pedimos explicitamente para a Twelve Data
            // devolver os horários no fuso de São Paulo.
            var url =
                $"https://{apiHost}/time_series" +
                $"?symbol={Uri.EscapeDataString(simbolo)}" +
                $"&interval={Uri.EscapeDataString(intervalo)}" +
                $"&outputsize={quantidade}" +
                $"&timezone={Uri.EscapeDataString("America/Sao_Paulo")}" +
                $"&format=JSON";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            request.Headers.Add(
                "x-rapidapi-key",
                apiKey);

            request.Headers.Add(
                "x-rapidapi-host",
                apiHost);

            using var response =
                await client.SendAsync(request);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Erro Twelve Data. Status: {Status}. Resposta: {Resposta}",
                    response.StatusCode,
                    json);

                throw new InvalidOperationException(
                    $"Erro ao consultar dados de mercado: {(int)response.StatusCode}.");
            }

            using var documento =
                JsonDocument.Parse(json);

            var raiz =
                documento.RootElement;

            if (raiz.TryGetProperty(
                    "status",
                    out var status) &&
                status.GetString()?.Equals(
                    "error",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                var mensagem =
                    raiz.TryGetProperty(
                        "message",
                        out var message)
                        ? message.GetString()
                        : "Erro desconhecido retornado pela API.";

                throw new InvalidOperationException(
                    mensagem ??
                    "Erro retornado pela API.");
            }

            if (!raiz.TryGetProperty(
                    "values",
                    out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "A API não retornou velas para o ativo selecionado.");
            }

            var velas =
                new List<VelaMercado>();

            foreach (var item in values.EnumerateArray())
            {
                if (!item.TryGetProperty(
                        "datetime",
                        out var datetimeElement))
                {
                    continue;
                }

                var dataHoraTexto =
                    datetimeElement.GetString();

                if (string.IsNullOrWhiteSpace(
                        dataHoraTexto))
                {
                    continue;
                }

                // Como solicitamos timezone=America/Sao_Paulo,
                // esse horário já representa o horário de Brasília.
                if (!DateTime.TryParseExact(
                        dataHoraTexto,
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dataHora))
                {
                    if (!DateTime.TryParse(
                            dataHoraTexto,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out dataHora))
                    {
                        continue;
                    }
                }

                velas.Add(
                    new VelaMercado
                    {
                        DataHora = dataHora,

                        Abertura =
                            LerDecimal(
                                item,
                                "open"),

                        Maxima =
                            LerDecimal(
                                item,
                                "high"),

                        Minima =
                            LerDecimal(
                                item,
                                "low"),

                        Fechamento =
                            LerDecimal(
                                item,
                                "close"),

                        Volume =
                            LerDecimal(
                                item,
                                "volume")
                    });
            }

            // Mantemos as velas em ordem cronológica:
            // da mais antiga para a mais recente.
            return velas
                .OrderBy(v => v.DataHora)
                .ToList();
        }

        private static decimal LerDecimal(
            JsonElement elemento,
            string propriedade)
        {
            if (!elemento.TryGetProperty(
                    propriedade,
                    out var valor))
            {
                return 0;
            }

            var texto =
                valor.GetString();

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var resultado))
            {
                return resultado;
            }

            return 0;
        }
    }
}