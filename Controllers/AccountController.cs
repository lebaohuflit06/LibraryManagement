using System.Security.Claims;
using LibraryManagement.Helpers;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();
    public AccountController(AppDbContext db) => _db = db;

    [HttpGet] public IActionResult Register() => View();

    // Đăng ký tài khoản
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng");
            return View(vm);
        }

        var user = new AppUser { FullName = vm.FullName.Trim(), Email = email, Role = AppRoles.Member };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SignIn(user);
        TempData["Msg"] = "Đăng ký thành công! Hãy đăng ký thẻ thư viện để mượn sách.";
        return RedirectToAction("Index", "LibraryCard");
    }

    [HttpGet] public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    // Đăng nhập tài khoản
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            return View(vm);
        }

        await SignIn(user);
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");

        // admin vào thẳng trang quản lý, member vào trang chủ
        if (user.Role == AppRoles.Admin) return RedirectToAction("Index", "AdminBooks");
        return RedirectToAction("Index", "Home");
    }

    // Đăng xuất
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private Task SignIn(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
