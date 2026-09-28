namespace VisaoDeAguia.Services
{
    public interface ITelegramService
    {
        Task<List<TelegramGrupo>> BuscarGruposAsync(string botToken);

        Task<(bool Sucesso, string Mensagem)> EnviarMensagemAsync(
            string botToken,
            string chatId,
            string mensagem);
    }

    public class TelegramGrupo
    {
        public long ChatId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;
    }
}