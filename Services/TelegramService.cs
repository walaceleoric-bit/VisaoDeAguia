using System.Net.Http.Json;
using System.Text.Json;

namespace VisaoDeAguia.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(
            HttpClient httpClient,
            ILogger<TelegramService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<TelegramGrupo>> BuscarGruposAsync(
            string botToken)
        {
            var grupos = new Dictionary<long, TelegramGrupo>();

            if (string.IsNullOrWhiteSpace(botToken))
                return grupos.Values.ToList();

            try
            {
                var token = botToken.Trim();

                var url =
                    $"https://api.telegram.org/bot{token}/getUpdates";

                using var response =
                    await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Telegram getUpdates retornou HTTP {StatusCode}.",
                        response.StatusCode);

                    return grupos.Values.ToList();
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                using var documento =
                    JsonDocument.Parse(json);

                var raiz = documento.RootElement;

                if (!raiz.TryGetProperty("ok", out var ok) ||
                    !ok.GetBoolean())
                {
                    return grupos.Values.ToList();
                }

                if (!raiz.TryGetProperty(
                        "result",
                        out var resultados))
                {
                    return grupos.Values.ToList();
                }

                foreach (var update in resultados.EnumerateArray())
                {
                    JsonElement mensagem;

                    if (update.TryGetProperty(
                            "message",
                            out var message))
                    {
                        mensagem = message;
                    }
                    else if (update.TryGetProperty(
                                 "channel_post",
                                 out var channelPost))
                    {
                        mensagem = channelPost;
                    }
                    else
                    {
                        continue;
                    }

                    if (!mensagem.TryGetProperty(
                            "chat",
                            out var chat))
                    {
                        continue;
                    }

                    if (!chat.TryGetProperty(
                            "id",
                            out var idElement))
                    {
                        continue;
                    }

                    var chatId = idElement.GetInt64();

                    var tipo = chat.TryGetProperty(
                        "type",
                        out var typeElement)
                        ? typeElement.GetString() ?? string.Empty
                        : string.Empty;

                    // Queremos apenas grupos.
                    if (tipo != "group" &&
                        tipo != "supergroup")
                    {
                        continue;
                    }

                    var nome = chat.TryGetProperty(
                        "title",
                        out var titleElement)
                        ? titleElement.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(nome))
                    {
                        nome = $"Grupo {chatId}";
                    }

                    grupos[chatId] = new TelegramGrupo
                    {
                        ChatId = chatId,
                        Nome = nome,
                        Tipo = tipo
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao buscar grupos do Telegram.");
            }

            return grupos.Values
                .OrderBy(g => g.Nome)
                .ToList();
        }

        public async Task<(bool Sucesso, string Mensagem)>
            EnviarMensagemAsync(
                string botToken,
                string chatId,
                string mensagem)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                return (
                    false,
                    "Informe o Token do Bot."
                );
            }

            if (string.IsNullOrWhiteSpace(chatId))
            {
                return (
                    false,
                    "Informe o Chat ID."
                );
            }

            if (string.IsNullOrWhiteSpace(mensagem))
            {
                return (
                    false,
                    "A mensagem está vazia."
                );
            }

            try
            {
                var token = botToken.Trim();

                var url =
                    $"https://api.telegram.org/bot{token}/sendMessage";

                var dados = new
                {
                    chat_id = chatId.Trim(),
                    text = mensagem
                };

                using var response =
                    await _httpClient.PostAsJsonAsync(
                        url,
                        dados);

                if (response.IsSuccessStatusCode)
                {
                    return (
                        true,
                        "Mensagem enviada com sucesso."
                    );
                }

                var respostaTelegram =
                    await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Erro ao enviar mensagem pelo Telegram. HTTP {StatusCode}. Resposta: {Resposta}",
                    response.StatusCode,
                    respostaTelegram);

                return (
                    false,
                    "O Telegram não conseguiu enviar a mensagem. Verifique o Token e o Chat ID."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao enviar mensagem pelo Telegram.");

                return (
                    false,
                    "Não foi possível conectar ao Telegram."
                );
            }
        }
    }
}