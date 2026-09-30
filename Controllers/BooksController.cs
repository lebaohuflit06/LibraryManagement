using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

public class BooksController : Controller
{
    private readonly AppDbContext _db;
    public BooksController(AppDbContext db) => _db = db;

    // Tìm kiếm sách (theo tên, tác giả, NXB, thể loại)
    public async Task<IActionResult> Index(string? q, string? category)
    {
        var all = await _db.Books.AsNoTracking().OrderBy(b => b.Title).ToListAsync();
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var key = TextHelper.Normalize(q);
            query = query.Where(b =>
                TextHelper.Normalize(b.Title).Contains(key) ||
                TextHelper.Normalize(b.Author).Contains(key) ||
                TextHelper.Normalize(b.Publisher).Contains(key));
        }

        return View(new BookListViewModel
        {
            Q = q,
            Category = category,
            Categories = all.Select(b => b.Category).Distinct().OrderBy(c => c).ToList(),
            Books = query.ToList()
        });
    }

    // Xem thông tin sách
    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        return book == null ? NotFound() : View(book);
    }
}
