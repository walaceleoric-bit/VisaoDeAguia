using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisaoDeAguia.Models;
using VisaoDeAguia.ViewModels;

namespace VisaoDeAguia.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private const string EmailDono =
            "walaceleoric@gmail.com";

        private readonly UserManager<Usuario> _userManager;

        public AdminController(
            UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        private bool EhDono()
        {
            var email =
                User.Identity?.Name;

            return
                !string.IsNullOrWhiteSpace(email) &&
                string.Equals(
                    email,
                    EmailDono,
                    StringComparison.OrdinalIgnoreCase);
        }

        // GET: /Admin
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!EhDono())
            {
                return Forbid();
            }

            var usuarios =
                await _userManager.Users
                    .AsNoTracking()
                    .OrderByDescending(
                        u => u.DataCadastro)
                    .Select(
                        u => new AdminUsuarioViewModel
                        {
                            Id =
                                u.Id,

                            Nome =
                                u.Nome,

                            Email =
                                u.Email ??
                                string.Empty,

                            Ativo =
                                u.Ativo,

                            DataCadastro =
                                u.DataCadastro
                        })
                    .ToListAsync();

            return View(usuarios);
        }

        // POST: /Admin/AlterarStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(
            string id)
        {
            if (!EhDono())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["AdminErro"] =
                    "Usuário inválido.";

                return RedirectToAction(
                    nameof(Index));
            }

            var usuario =
                await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                TempData["AdminErro"] =
                    "Usuário não encontrado.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.Equals(
                    usuario.Email,
                    EmailDono,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["AdminErro"] =
                    "A conta do dono não pode ser bloqueada.";

                return RedirectToAction(
                    nameof(Index));
            }

            usuario.Ativo =
                !usuario.Ativo;

            var resultado =
                await _userManager.UpdateAsync(
                    usuario);

            if (!resultado.Succeeded)
            {
                TempData["AdminErro"] =
                    ObterErros(resultado);

                return RedirectToAction(
                    nameof(Index));
            }

            if (!usuario.Ativo)
            {
                await _userManager
                    .UpdateSecurityStampAsync(
                        usuario);
            }

            TempData["AdminSucesso"] =
                usuario.Ativo
                    ? "Usuário liberado com sucesso."
                    : "Usuário bloqueado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // POST: /Admin/AlterarDados
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarDados(
            string id,
            string nome,
            string email)
        {
            if (!EhDono())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["AdminErro"] =
                    "Usuário inválido.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(nome) ||
                string.IsNullOrWhiteSpace(email))
            {
                TempData["AdminErro"] =
                    "Nome e e-mail são obrigatórios.";

                return RedirectToAction(
                    nameof(Index));
            }

            var usuario =
                await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                TempData["AdminErro"] =
                    "Usuário não encontrado.";

                return RedirectToAction(
                    nameof(Index));
            }

            nome =
                nome.Trim();

            email =
                email.Trim();

            var alterandoContaDono =
                string.Equals(
                    usuario.Email,
                    EmailDono,
                    StringComparison.OrdinalIgnoreCase);

            if (alterandoContaDono &&
                !string.Equals(
                    email,
                    EmailDono,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["AdminErro"] =
                    "O e-mail da conta do dono não pode ser alterado neste painel.";

                return RedirectToAction(
                    nameof(Index));
            }

            var usuarioComEmail =
                await _userManager
                    .FindByEmailAsync(email);

            if (usuarioComEmail != null &&
                usuarioComEmail.Id != usuario.Id)
            {
                TempData["AdminErro"] =
                    "Este e-mail já está sendo utilizado por outro usuário.";

                return RedirectToAction(
                    nameof(Index));
            }

            usuario.Nome =
                nome;

            var resultadoEmail =
                await _userManager.SetEmailAsync(
                    usuario,
                    email);

            if (!resultadoEmail.Succeeded)
            {
                TempData["AdminErro"] =
                    ObterErros(
                        resultadoEmail);

                return RedirectToAction(
                    nameof(Index));
            }

            var resultadoLogin =
                await _userManager.SetUserNameAsync(
                    usuario,
                    email);

            if (!resultadoLogin.Succeeded)
            {
                TempData["AdminErro"] =
                    ObterErros(
                        resultadoLogin);

                return RedirectToAction(
                    nameof(Index));
            }

            var resultado =
                await _userManager.UpdateAsync(
                    usuario);

            if (!resultado.Succeeded)
            {
                TempData["AdminErro"] =
                    ObterErros(
                        resultado);

                return RedirectToAction(
                    nameof(Index));
            }

            TempData["AdminSucesso"] =
                "Dados do usuário atualizados com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // POST: /Admin/AlterarSenha
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarSenha(
            string id,
            string novaSenha)
        {
            if (!EhDono())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["AdminErro"] =
                    "Usuário inválido.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    novaSenha))
            {
                TempData["AdminErro"] =
                    "Informe a nova senha.";

                return RedirectToAction(
                    nameof(Index));
            }

            var usuario =
                await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                TempData["AdminErro"] =
                    "Usuário não encontrado.";

                return RedirectToAction(
                    nameof(Index));
            }

            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(
                        usuario);

            var resultado =
                await _userManager
                    .ResetPasswordAsync(
                        usuario,
                        token,
                        novaSenha);

            if (!resultado.Succeeded)
            {
                TempData["AdminErro"] =
                    ObterErros(
                        resultado);

                return RedirectToAction(
                    nameof(Index));
            }

            await _userManager
                .UpdateSecurityStampAsync(
                    usuario);

            TempData["AdminSucesso"] =
                "Senha alterada com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // POST: /Admin/ExcluirUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirUsuario(
            string id)
        {
            if (!EhDono())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["AdminErro"] =
                    "Usuário inválido.";

                return RedirectToAction(
                    nameof(Index));
            }

            var usuario =
                await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                TempData["AdminErro"] =
                    "Usuário não encontrado.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.Equals(
                    usuario.Email,
                    EmailDono,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["AdminErro"] =
                    "A conta do dono não pode ser excluída.";

                return RedirectToAction(
                    nameof(Index));
            }

            var nomeUsuario =
                string.IsNullOrWhiteSpace(usuario.Nome)
                    ? usuario.Email
                    : usuario.Nome;

            var resultado =
                await _userManager.DeleteAsync(
                    usuario);

            if (!resultado.Succeeded)
            {
                TempData["AdminErro"] =
                    ObterErros(resultado);

                return RedirectToAction(
                    nameof(Index));
            }

            TempData["AdminSucesso"] =
                $"Usuário {nomeUsuario} excluído com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        private static string ObterErros(
            IdentityResult resultado)
        {
            var erros =
                resultado.Errors
                    .Select(
                        e => e.Description)
                    .Where(
                        e =>
                            !string.IsNullOrWhiteSpace(
                                e))
                    .ToList();

            if (erros.Count == 0)
            {
                return
                    "Não foi possível realizar a alteração.";
            }

            return string.Join(
                " ",
                erros);
        }
    }
}