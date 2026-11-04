using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[Authorize]
public class LibraryCardController : Controller
{
    private readonly AppDbContext _db;
    public LibraryCardController(AppDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var card = await _db.Cards.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == UserId);
        return card == null ? RedirectToAction(nameof(Register)) : View(card);
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (await _db.Cards.AnyAsync(c => c.UserId == UserId)) return RedirectToAction(nameof(Index));
        return View(new CardRegisterViewModel());
    }

    // Đăng ký thẻ thư viện
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CardRegisterViewModel vm)
    {
        if (await _db.Cards.AnyAsync(c => c.UserId == UserId)) return RedirectToAction(nameof(Index));

        if (vm.DateOfBirth is { } dob && dob > DateTime.Today.AddYears(-6))
            ModelState.AddModelError(nameof(vm.DateOfBirth), "Độc giả phải từ 6 tuổi trở lên");
        if (await _db.Cards.AnyAsync(c => c.IdNumber == vm.IdNumber))
            ModelState.AddModelError(nameof(vm.IdNumber), "Số CCCD này đã đăng ký thẻ");
        if (!ModelState.IsValid) return View(vm);

        var card = new LibraryCard
        {
            UserId = UserId,
            Phone = vm.Phone,
            Address = vm.Address.Trim(),
            IdNumber = vm.IdNumber,
            DateOfBirth = vm.DateOfBirth!.Value,
            IssuedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddYears(1),
            CardNumber = "TMP"
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync();
        card.CardNumber = $"TV{DateTime.Now:yy}{card.Id:D5}";
        await _db.SaveChangesAsync();

        TempData["Msg"] = "Đăng ký thẻ thư viện thành công!";
        return RedirectToAction(nameof(Index));
    }
}
