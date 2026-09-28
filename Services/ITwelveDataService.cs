using VisaoDeAguia.Models;

namespace VisaoDeAguia.Services
{
    public interface ITwelveDataService
    {
        Task<List<VelaMercado>> ObterVelasAsync(
            string simbolo,
            string intervalo,
            int quantidade = 100);
    }
}