using Microsoft.AspNetCore.Identity;

namespace VisaoDeAguia.Models
{
    public class Usuario : IdentityUser
    {
        public string Nome { get; set; } = string.Empty;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;
    }
}