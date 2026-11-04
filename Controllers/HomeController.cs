using LibraryManagement.Data;
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
            NewBooks = await _db.Books.OrderByDescending(b => b.Id).Take(4).ToListAsync(),
            Categories = await _db.Books.Select(b => b.Category).Distinct().ToListAsync(),
            BookCount = await _db.Books.SumAsync(b => b.Quantity),
            MemberCount = await _db.Users.CountAsync()
        };
        return View(vm);
    }
}
