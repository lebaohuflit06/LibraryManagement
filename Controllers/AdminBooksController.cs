using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace LibraryManagement.Controllers
{
    // quản lý mã sách (tên, tác giả, thể loại, nxb, năm...)
    // quản lý các cuốn cụ thể là AdminCopiesController.
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminBooksController : Controller
    {
        private readonly AppDbContext _db;
        public AdminBooksController(AppDbContext db) => _db = db;

        // danh sách mã sách + tìm kiếm (read)
        public async Task<IActionResult> Index(string? q)
        {
            var books = await _db.Books.AsNoTracking().Include(b => b.Copies).OrderBy(b => b.Title).ToListAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                books = books.Where(b =>
                TextHelper.Normalize(b.Title).Contains(key) ||
                TextHelper.Normalize(b.Author).Contains(key) ||
                TextHelper.Normalize(b.Publisher).Contains(key) ||
                TextHelper.Normalize(b.Category).Contains(key)).ToList();
            }
            ViewBag.Q = q;
            return View(books);
        }

        // thêm mã sách (create)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategories();
            return View("Form", new BookFormViewModel { Year = DateTime.Now.Year });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategories();
                return View("Form", vm);
            }
            var book = new Book();
            Apply(book, vm);
            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã thêm sách \"{book.Title}\". Hãy thêm số lượng cuốn cho sách này.";
            return RedirectToAction("Index", "AdminCopies", new { bookId = book.Id });
        }

        // sửa mã sách (Update)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            await LoadCategories();
            return View("Form", new BookFormViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Category = book.Category,
                Publisher = book.Publisher,
                Year = book.Year,
                Pages = book.Pages,
                Price = book.Price,
                Description = book.Description
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BookFormViewModel vm)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == vm.Id);
            if (book == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadCategories();
                return View("Form", vm);
            }

            Apply(book, vm);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã cập nhật sách \"{book.Title}\".";
            return RedirectToAction(nameof(Index));
        }

        // xóa mã sách (delete), chỉ khi ko còn đầu sách nào
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            if (await _db.BookCopies.AnyAsync(c => c.BookId == id))
            {
                TempData["Err"] = $"Không thể xóa \"{book.Title}\" vì sách vẫn còn đầu sách.";
                return RedirectToAction(nameof(Index));
            }

            _db.Books.Remove(book);
            await _db.SaveChangesAsync();
            TempData["Msg"] = $"Đã xóa sách \"{book.Title}\".";
            return RedirectToAction(nameof(Index));
        }

        // chép dữ liệu từ form sang entity (không chép id)
        private static void Apply(Book book, BookFormViewModel vm)
        {
            book.Title = vm.Title.Trim();
            book.Author = vm.Author.Trim();
            book.Category = vm.Category.Trim();
            book.Publisher = vm.Publisher.Trim();
            book.Year = vm.Year!.Value;
            book.Pages = vm.Pages!.Value;
            book.Price = vm.Price!.Value;
            book.Description = vm.Description?.Trim() ?? "";
        }

        private async Task LoadCategories()
        {
            ViewBag.Categories = await _db.Books.Select(b => b.Category).Distinct().OrderBy(c => c).ToListAsync();
        }
    }
}
