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
                return View(model);

            var usuario = await _userManager.FindByEmailAsync(model.Email);

            if (usuario == null || !usuario.Ativo)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(model);
            }

            var resultado = await _signInManager.PasswordSignInAsync(
                usuario,
                model.Senha,
                model.LembrarMe,
                lockoutOnFailure: false);

            if (!resultado.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Conta/Cadastro
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Cadastro()
        {
            return View(new CadastroViewModel());
        }

        // POST: /Conta/Cadastro
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(CadastroViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var existente = await _userManager.FindByEmailAsync(email);

            if (existente != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Já existe uma conta cadastrada com este e-mail.");

                return View(model);
            }

            var usuario = new Usuario
            {
                Nome = model.Nome.Trim(),
                UserName = email,
                Email = email,
                Ativo = true,
                DataCadastro = DateTime.UtcNow
            };

            var resultado = await _userManager.CreateAsync(
                usuario,
                model.Senha);

            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }

                return View(model);
            }

            await _signInManager.SignInAsync(
                usuario,
                isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // POST: /Conta/Sair
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sair()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
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