namespace VisaoDeAguia.Models
{
    public class ResultadoAnalise
    {
        public string Simbolo { get; set; } = string.Empty;

        public string Direcao { get; set; } = "AGUARDAR";

        public int Pontuacao { get; set; }

        public string Forca { get; set; } = "SEM SINAL";

        public decimal PrecoAtual { get; set; }

        public DateTime DataHora { get; set; }

        // Tendências principais
        public string Tendencia2H { get; set; } = "Neutra";

        public string Tendencia1H { get; set; } = "Neutra";

        public string Estrutura30M { get; set; } = "Neutra";

        // Pullback e confirmação
        public string Pullback15M { get; set; } = "Não confirmado";

        public string Confirmacao5M { get; set; } = "Aguardando";

        // Médias móveis
        public decimal Ema10_2H { get; set; }

        public decimal Ema20_2H { get; set; }

        public decimal Ema10_1H { get; set; }

        public decimal Ema20_1H { get; set; }

        public decimal Ema10_30M { get; set; }

        public decimal Ema20_30M { get; set; }

        public decimal Ema10_15M { get; set; }

        public decimal Ema20_15M { get; set; }

        public decimal Ema10_5M { get; set; }

        public decimal Ema20_5M { get; set; }

        // Suporte e resistência
        public decimal Suporte { get; set; }

        public decimal Resistencia { get; set; }

        // Informações adicionais do cenário
        public bool TendenciasAlinhadas { get; set; }

        public bool ProximoSuporte { get; set; }

        public bool ProximoResistencia { get; set; }

        public bool PullbackConfirmado { get; set; }

        public bool GatilhoConfirmado { get; set; }

        // Explicações da análise
        public List<string> Motivos { get; set; } = new();
    }
}