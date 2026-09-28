namespace VisaoDeAguia.Models
{
    public class SaldoRoboViewModel
    {
        public int LimiteMinutos { get; set; }

        public int MinutosUtilizados { get; set; }

        public int MinutosDisponiveis { get; set; }

        public double PercentualDisponivel { get; set; }
    }
}