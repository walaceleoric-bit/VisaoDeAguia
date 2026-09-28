using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public interface IAnaliseMercadoService
    {
        Task<ResultadoAnalise> AnalisarAsync(string simbolo);
    }
}