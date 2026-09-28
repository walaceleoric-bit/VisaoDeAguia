namespace VisaoDeAguia.Models
{
    public class VelaMercado
    {
        public DateTime DataHora { get; set; }

        public decimal Abertura { get; set; }

        public decimal Maxima { get; set; }

        public decimal Minima { get; set; }

        public decimal Fechamento { get; set; }

        public decimal Volume { get; set; }
    }
}