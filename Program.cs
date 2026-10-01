using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Data;
using VisaoDeAguia.Models;
using VisaoDeAguia.Services;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados PostgreSQL
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity
builder.Services
    .AddIdentity<Usuario, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Configuração do cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Login";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Cliente HTTP da Twelve Data
builder.Services.AddHttpClient("TwelveData", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Serviço de dados de mercado
builder.Services.AddScoped<ITwelveDataService, TwelveDataService>();

// Serviço de análise de mercado
builder.Services.AddScoped<IAnaliseMercadoService, AnaliseMercadoService>();

// Última análise realizada pelo robô.
// Singleton = uma única instância compartilhada
// entre o robô automático e os controllers.
builder.Services.AddSingleton<
    IUltimaAnaliseService,
    UltimaAnaliseService>();

// Serviço do Telegram
builder.Services.AddHttpClient<ITelegramService, TelegramService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Robô automático em segundo plano
builder.Services.AddHostedService<RoboMercadoBackgroundService>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();