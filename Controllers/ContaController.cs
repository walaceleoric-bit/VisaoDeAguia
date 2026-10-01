using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VisaoDeAguia.Models;
using VisaoDeAguia.ViewModels;

namespace VisaoDeAguia.Controllers
{
    public class ContaController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public ContaController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Conta/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        // POST: /Conta/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario =
                await _userManager.FindByEmailAsync(
                    model.Email.Trim());

            if (usuario == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-mail ou senha inválidos.");

                return View(model);
            }

            if (!usuario.Ativo)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Seu cadastro ainda não foi liberado pelo administrador. " +
                    "Entre em contato pelo WhatsApp: (27) 98843-3016.");

                return View(model);
            }

            var resultado =
                await _signInManager.PasswordSignInAsync(
                    usuario,
                    model.Senha,
                    model.LembrarMe,
                    lockoutOnFailure: false);

            if (!resultado.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-mail ou senha inválidos.");

                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home");
        }

        // GET: /Conta/Cadastro
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Cadastro()
        {
            return View(
                new CadastroViewModel());
        }

        // POST: /Conta/Cadastro
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(
            CadastroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim();

            var existente =
                await _userManager.FindByEmailAsync(
                    email);

            if (existente != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Já existe uma conta cadastrada com este e-mail.");

                return View(model);
            }

            var usuario =
                new Usuario
                {
                    Nome =
                        model.Nome.Trim(),

                    UserName =
                        email,

                    Email =
                        email,

                    // Todo novo cadastro precisa
                    // ser liberado pelo administrador.
                    Ativo =
                        false,

                    DataCadastro =
                        DateTime.UtcNow
                };

            var resultado =
                await _userManager.CreateAsync(
                    usuario,
                    model.Senha);

            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        erro.Description);
                }

                return View(model);
            }

            // Não realiza login automático.
            // O administrador precisa liberar
            // o usuário primeiro.

            TempData["CadastroSucesso"] =
                "Cadastro realizado com sucesso! " +
                "Seu acesso está aguardando liberação. " +
                "Avise pelo WhatsApp (27) 98843-3016 " +
                "que você concluiu o cadastro.";

            return RedirectToAction(
                nameof(Login));
        }

        // POST: /Conta/Sair
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sair()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }

        // GET: /Conta/AcessoNegado
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }
    }
}