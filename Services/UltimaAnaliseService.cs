using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public interface IUltimaAnaliseService
    {
        ResultadoAnalise? Obter();

        void Atualizar(
            ResultadoAnalise resultado);
    }

    public class UltimaAnaliseService :
        IUltimaAnaliseService
    {
        private readonly object _bloqueio =
            new();

        private ResultadoAnalise? _ultimaAnalise;

        public ResultadoAnalise? Obter()
        {
            lock (_bloqueio)
            {
                return _ultimaAnalise;
            }
        }

        public void Atualizar(
            ResultadoAnalise resultado)
        {
            lock (_bloqueio)
            {
                _ultimaAnalise = resultado;
            }
        }
    }
}