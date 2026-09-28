using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisaoDeAguia.Models
{
    public class ConfiguracaoRobo
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        // Telegram
        public string? TelegramBotToken { get; set; }

        public string? TelegramChatId { get; set; }

        public bool TelegramAtivo { get; set; } = false;

        // Mercados
        public bool AnalisarForex { get; set; } = true;

        public bool AnalisarAcoes { get; set; } = true;

        public bool AnalisarCriptomoedas { get; set; } = true;

        // Sinais
        public bool ReceberSinalForte { get; set; } = true;

        public bool ReceberSinalModerado { get; set; } = false;

        public int PontuacaoMinima { get; set; } = 80;

        // Horário de funcionamento do robô
        [Required]
        public TimeSpan HorarioInicio { get; set; } =
            new TimeSpan(8, 30, 0);

        [Required]
        public TimeSpan HorarioFim { get; set; } =
            new TimeSpan(11, 0, 0);

        public DateTime DataAtualizacao { get; set; } =
            DateTime.UtcNow;
    }
}