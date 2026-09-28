using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisaoDeAguia.Models
{
    public class SinalEnviado
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required]
        [MaxLength(30)]
        public string Simbolo { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Direcao { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Forca { get; set; } = string.Empty;

        public int Pontuacao { get; set; }

        public decimal Preco { get; set; }

        // Horário da vela de 5 minutos que gerou o sinal.
        // Será usado para impedir o mesmo sinal de ser enviado novamente.
        public DateTime DataHoraVela { get; set; }

        // Momento em que a mensagem foi enviada ao Telegram.
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    }
}