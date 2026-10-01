namespace VisaoDeAguia.ViewModels
{
    public class AdminUsuarioViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public DateTime DataCadastro { get; set; }
    }
}