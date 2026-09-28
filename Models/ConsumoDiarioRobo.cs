using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisaoDeAguia.Models
{
    public class ConsumoDiarioRobo
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        // Data referente ao consumo no horário de Brasília.
        public DateTime Data { get; set; }

        // Quantidade de minutos utilizados no dia.
        public int MinutosUtilizados { get; set; } = 0;
    }
}