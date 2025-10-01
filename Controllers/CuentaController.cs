using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TiendaPc.Controllers;

public class CuentaController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public CuentaController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Cuenta/Login
    [HttpGet("/Cuenta/Login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    // POST: /Cuenta/Login
    [HttpPost("/Cuenta/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "Credenciales inválidas.");
        return View(vm);
    }

    // GET: /Cuenta/Registro
    [HttpGet("/Cuenta/Registro")]
    public IActionResult Registro(string? returnUrl = null) => View(new RegistroVm { ReturnUrl = returnUrl });

    // POST: /Cuenta/Registro
    [HttpPost("/Cuenta/Registro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new IdentityUser { UserName = vm.Email, Email = vm.Email };
        var result = await _userManager.CreateAsync(user, vm.Password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        foreach (var e in result.Errors)
            ModelState.AddModelError("", e.Description);

        return View(vm);
    }

    // POST: /Cuenta/Logout
    [Authorize]
    [HttpPost("/Cuenta/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class RegistroVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password), MinLength(6)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
