using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Data;
using VisaoDeAguia.Models;

namespace VisaoDeAguia.ViewComponents
{
    public class SaldoRoboViewComponent : ViewComponent
    {
        private const int LimiteDiarioMinutos = 150;

        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public SaldoRoboViewComponent(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var usuarioId =
                _userManager.GetUserId(
                    HttpContext.User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Content(string.Empty);
            }

            var fuso =
                ObterFusoHorario();

            var agoraUtc =
                DateTime.UtcNow;

            var agoraBrasilia =
                TimeZoneInfo.ConvertTimeFromUtc(
                    agoraUtc,
                    fuso);

            var inicioDiaBrasilia =
                DateTime.SpecifyKind(
                    agoraBrasilia.Date,
                    DateTimeKind.Unspecified);

            var inicioDiaUtc =
                TimeZoneInfo.ConvertTimeToUtc(
                    inicioDiaBrasilia,
                    fuso);

            var consumo =
                await _context.ConsumosDiariosRobo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.UsuarioId == usuarioId &&
                        c.Data == inicioDiaUtc);

            var utilizados =
                consumo?.MinutosUtilizados ?? 0;

            utilizados =
                Math.Clamp(
                    utilizados,
                    0,
                    LimiteDiarioMinutos);

            var disponiveis =
                LimiteDiarioMinutos - utilizados;

            var percentualDisponivel =
                LimiteDiarioMinutos == 0
                    ? 0
                    : disponiveis * 100.0 /
                      LimiteDiarioMinutos;

            var model =
                new SaldoRoboViewModel
                {
                    LimiteMinutos =
                        LimiteDiarioMinutos,

                    MinutosUtilizados =
                        utilizados,

                    MinutosDisponiveis =
                        disponiveis,

                    PercentualDisponivel =
                        percentualDisponivel
                };

            return View(model);
        }

        private static TimeZoneInfo ObterFusoHorario()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Sao_Paulo");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "E. South America Standard Time");
            }
        }
    }
}