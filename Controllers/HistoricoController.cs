using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Data;
using VisaoDeAguia.Models;

namespace VisaoDeAguia.Controllers
{
    [Authorize]
    public class HistoricoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<HistoricoController> _logger;

        private const int TamanhoPagina = 10;

        public HistoricoController(
            AppDbContext context,
            UserManager<Usuario> userManager,
            ILogger<HistoricoController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? data,
            int? mes,
            int? ano,
            int pagina = 1)
        {
            try
            {
                var usuarioId = _userManager.GetUserId(User);

                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    return RedirectToAction(
                        "Login",
                        "Conta");
                }

                if (pagina < 1)
                    pagina = 1;

                var consulta = _context.SinaisEnviados
                    .AsNoTracking()
                    .Where(s => s.UsuarioId == usuarioId);

                // Filtro por dia específico.
                if (data.HasValue)
                {
                    var inicioBrasilia =
                        DateTime.SpecifyKind(
                            data.Value.Date,
                            DateTimeKind.Unspecified);

                    var fimBrasilia =
                        inicioBrasilia.AddDays(1);

                    var inicioUtc =
                        ConverterBrasiliaParaUtc(
                            inicioBrasilia);

                    var fimUtc =
                        ConverterBrasiliaParaUtc(
                            fimBrasilia);

                    consulta = consulta.Where(s =>
                        s.DataHoraVela >= inicioUtc &&
                        s.DataHoraVela < fimUtc);
                }
                // Se não houver dia, permite filtrar por mês/ano.
                else if (mes.HasValue &&
                         ano.HasValue &&
                         mes.Value >= 1 &&
                         mes.Value <= 12)
                {
                    var inicioBrasilia =
                        new DateTime(
                            ano.Value,
                            mes.Value,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Unspecified);

                    var fimBrasilia =
                        inicioBrasilia.AddMonths(1);

                    var inicioUtc =
                        ConverterBrasiliaParaUtc(
                            inicioBrasilia);

                    var fimUtc =
                        ConverterBrasiliaParaUtc(
                            fimBrasilia);

                    consulta = consulta.Where(s =>
                        s.DataHoraVela >= inicioUtc &&
                        s.DataHoraVela < fimUtc);
                }

                var totalRegistros =
                    await consulta.CountAsync();

                var totalPaginas =
                    (int)Math.Ceiling(
                        totalRegistros /
                        (double)TamanhoPagina);

                if (totalPaginas > 0 &&
                    pagina > totalPaginas)
                {
                    pagina = totalPaginas;
                }

                var sinais = await consulta
                    .OrderByDescending(s => s.DataHoraVela)
                    .ThenByDescending(s => s.DataEnvio)
                    .Skip((pagina - 1) * TamanhoPagina)
                    .Take(TamanhoPagina)
                    .ToListAsync();

                ViewBag.TotalRegistros =
                    totalRegistros;

                ViewBag.PaginaAtual =
                    pagina;

                ViewBag.TotalPaginas =
                    totalPaginas;

                ViewBag.DataFiltro =
                    data?.ToString("yyyy-MM-dd");

                ViewBag.MesFiltro =
                    mes;

                ViewBag.AnoFiltro =
                    ano;

                return View(sinais);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao carregar o histórico de sinais.");

                ViewBag.Erro =
                    "Não foi possível carregar o histórico de sinais.";

                ViewBag.TotalRegistros = 0;
                ViewBag.PaginaAtual = 1;
                ViewBag.TotalPaginas = 0;

                return View(
                    new List<SinalEnviado>());
            }
        }

        private static DateTime ConverterBrasiliaParaUtc(
            DateTime horarioBrasilia)
        {
            try
            {
                var fuso =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "America/Sao_Paulo");

                var horarioSemFuso =
                    DateTime.SpecifyKind(
                        horarioBrasilia,
                        DateTimeKind.Unspecified);

                return TimeZoneInfo.ConvertTimeToUtc(
                    horarioSemFuso,
                    fuso);
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    var fuso =
                        TimeZoneInfo.FindSystemTimeZoneById(
                            "E. South America Standard Time");

                    var horarioSemFuso =
                        DateTime.SpecifyKind(
                            horarioBrasilia,
                            DateTimeKind.Unspecified);

                    return TimeZoneInfo.ConvertTimeToUtc(
                        horarioSemFuso,
                        fuso);
                }
                catch
                {
                    return DateTime.SpecifyKind(
                        horarioBrasilia.AddHours(3),
                        DateTimeKind.Utc);
                }
            }
        }
    }
}