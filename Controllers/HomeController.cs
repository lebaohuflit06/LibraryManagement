using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    // Xem trang chủ
    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            NewBooks = await _db.Books.AsNoTracking().Include(b => b.Copies).OrderByDescending(b => b.Id).Take(4).ToListAsync(),
            Categories = await _db.Books.Select(b => b.Category).Distinct().OrderBy(c => c).ToListAsync(),
            BookCount = await _db.BookCopies.CountAsync(c => c.Status != BookStatus.Liquidated),
            MemberCount = await _db.Users.CountAsync(u => u.Role == AppRoles.Member)
        };
        return View(vm);
    }
}
