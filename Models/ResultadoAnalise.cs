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


        // ==========================================
        // TENDÊNCIAS PRINCIPAIS
        // ==========================================

        public string Tendencia2H { get; set; } = "Neutra";

        public string Tendencia1H { get; set; } = "Neutra";

        public string Estrutura30M { get; set; } = "Neutra";


        // ==========================================
        // PULLBACK E CONFIRMAÇÃO
        // ==========================================

        public string Pullback15M { get; set; } =
            "Não confirmado";

        public string Confirmacao5M { get; set; } =
            "Aguardando";


        // ==========================================
        // EMAs ANTIGAS
        // Mantidas para compatibilidade
        // ==========================================

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


        // ==========================================
        // MÉDIAS MÓVEIS
        // NOVA ESTRUTURA 10 / 50 / 200
        // ==========================================

        public decimal Media10_5M { get; set; }

        public decimal Media50_5M { get; set; }

        public decimal Media200_5M { get; set; }

        public decimal Media10_15M { get; set; }

        public decimal Media50_15M { get; set; }

        public decimal Media200_15M { get; set; }

        public decimal Media10_30M { get; set; }

        public decimal Media50_30M { get; set; }

        public decimal Media200_30M { get; set; }

        public decimal Media10_1H { get; set; }

        public decimal Media50_1H { get; set; }

        public decimal Media200_1H { get; set; }

        public decimal Media10_2H { get; set; }

        public decimal Media50_2H { get; set; }

        public decimal Media200_2H { get; set; }


        // ==========================================
        // BANDAS DE BOLLINGER 20,2
        // Usadas principalmente no 5M
        // ==========================================

        public decimal BollingerSuperior5M { get; set; }

        public decimal BollingerMedia5M { get; set; }

        public decimal BollingerInferior5M { get; set; }

        public string SituacaoBollinger5M { get; set; } =
            "Neutra";


        // ==========================================
        // RSI 14
        // ==========================================

        public decimal Rsi14_5M { get; set; }

        public decimal Rsi14_15M { get; set; }

        public string SituacaoRsi { get; set; } =
            "Neutra";


        // ==========================================
        // MACD 12,26,9
        // ==========================================

        public decimal Macd5M { get; set; }

        public decimal MacdSinal5M { get; set; }

        public decimal MacdHistograma5M { get; set; }

        public string SituacaoMacd { get; set; } =
            "Neutra";


        // ==========================================
        // SUPORTE E RESISTÊNCIA
        // ==========================================

        public decimal Suporte { get; set; }

        public decimal Resistencia { get; set; }


        // ==========================================
        // INFORMAÇÕES DO CENÁRIO
        // ==========================================

        public bool TendenciasAlinhadas { get; set; }

        public bool ProximoSuporte { get; set; }

        public bool ProximoResistencia { get; set; }

        public bool PullbackConfirmado { get; set; }

        public bool GatilhoConfirmado { get; set; }


        // ==========================================
        // CONFIRMAÇÕES DOS NOVOS INDICADORES
        // ==========================================

        public bool MediasConfirmamCompra { get; set; }

        public bool MediasConfirmamVenda { get; set; }

        public bool BollingerConfirmaCompra { get; set; }

        public bool BollingerConfirmaVenda { get; set; }

        public bool RsiConfirmaCompra { get; set; }

        public bool RsiConfirmaVenda { get; set; }

        public bool MacdConfirmaCompra { get; set; }

        public bool MacdConfirmaVenda { get; set; }


        // ==========================================
        // EXPLICAÇÕES DA ANÁLISE
        // ==========================================

        public List<string> Motivos { get; set; } =
            new();
    }
}