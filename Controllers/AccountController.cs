using System.Security.Claims;
using LibraryManagement.Helpers;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();
    public AccountController(AppDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

        var user = new AppUser { FullName = vm.FullName.Trim(), Email = email, Role = AppRoles.Member, LastLoginAt = DateTime.Now };
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

        // đúng mật khẩu rồi mới báo tài khoản bị khóa (tránh lộ thông tin cho người không biết mật khẩu)
        if (user.IsLocked)
        {
            var reason = string.IsNullOrWhiteSpace(user.LockReason) ? "" : $" Lý do: {user.LockReason}.";
            ModelState.AddModelError("", "Tài khoản đã bị khóa." + reason + " Vui lòng liên hệ thư viện để được hỗ trợ.");
            return View(vm);
        }

        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await SignIn(user);

        if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);

        // admin / thủ thư vào thẳng trang quản lý, member vào trang chủ
        if (AppRoles.IsStaff(user.Role)) return RedirectToAction("Index", "AdminTracking");
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

    // ---------- tài khoản của tôi ----------
    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var vm = await BuildProfile(new ProfileViewModel());
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel input)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            var back = await BuildProfile(input);
            return View(back);
        }

        user.FullName = input.FullName.Trim();
        await _db.SaveChangesAsync();
        await SignIn(user); // cập nhật tên hiển thị trên thanh menu

        TempData["Msg"] = "Đã cập nhật thông tin tài khoản.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() =>
        View(new ChangePasswordViewModel { Forced = User.FindFirstValue(AuthHelper.MustChangePasswordClaim) == "1" });

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        vm.Forced = User.FindFirstValue(AuthHelper.MustChangePasswordClaim) == "1";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(vm.CurrentPassword) &&
            _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.CurrentPassword) == PasswordVerificationResult.Failed)
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng");
        if (!string.IsNullOrEmpty(vm.NewPassword) && vm.NewPassword == vm.CurrentPassword)
            ModelState.AddModelError(nameof(vm.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại");
        if (!ModelState.IsValid) return View(vm);

        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        await SignIn(user); // bỏ cờ "buộc đổi mật khẩu" khỏi phiên đăng nhập

        TempData["Msg"] = "Đổi mật khẩu thành công.";
        return RedirectToAction("Index", AppRoles.IsStaff(user.Role) ? "AdminTracking" : "Home");
    }

    private async Task<ProfileViewModel?> BuildProfile(ProfileViewModel vm)
    {
        var user = await _db.Users.AsNoTracking().Include(u => u.Card).FirstOrDefaultAsync(u => u.Id == UserId);
        if (user == null) return null;
        vm.Email = user.Email;
        vm.Role = user.Role;
        vm.CreatedAt = user.CreatedAt;
        vm.LastLoginAt = user.LastLoginAt;
        vm.CardNumber = user.Card?.CardNumber;
        if (string.IsNullOrEmpty(vm.FullName)) vm.FullName = user.FullName;
        return vm;
    }

    private Task SignIn(AppUser user) =>
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AuthHelper.BuildPrincipal(user));
}
