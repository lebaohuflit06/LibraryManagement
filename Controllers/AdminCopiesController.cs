using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LibraryManagement.Controllers
{
    // quản lý ĐẦU SÁCH: TỪNG CUỐN của một mã sách (số kiểm soát 001, 002, ... tình trạng và ghi chú)
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminCopiesController : Controller
    {
        private readonly AppDbContext _db;
        public AdminCopiesController(AppDbContext db) => _db = db;

        // danh sách đầu sách của một mã sách (read)
        public async Task<IActionResult> Index(int bookId)
        {
            var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null) return NotFound();
            return View(new CopyCreateViewModel { BookId = bookId, Count = 1, BookTitle = book.Title });
        }


        // thêm đầu sách (create), thêm 1 hoặc nhiều cuốn, số kiểm soát tự đánh tiếp theo
        [HttpGet]
        public async Task<IActionResult> Create(int bookId)
        {
            var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null) return NotFound();
            return View(new CopyCreateViewModel { BookId = bookId, Count = 1, BookTitle = book.Title });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CopyCreateViewModel vm)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == vm.BookId);
            if (book == null) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.BookTitle = book.Title;
                return View(vm);
            }

            // số kiểm soát lớn nhất hiện có, kiểu int? để khi chưa có cuốn nào và trả về null thay vì lỗi
            var last = await _db.BookCopies.Where(c => c.BookId == book.Id).MaxAsync(c => (int?)c.ControlNo) ?? 0;
            var count = vm.Count!.Value;
            for (var i = 1; i <= count; i++)
            {
                var copy = new BookCopy
                {
                    BookId = book.Id,
                    ControlNo = last + i,
                    Status = BookStatus.Available,
                    StatusChangedAt = DateTime.Now,
                    Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim()
                };
                _db.BookCopies.Add(copy);
                Logs.Copy(_db, copy, null, BookStatus.Available, "Nhập kho", User.Identity?.Name, null, copy.Note);
            }
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã thêm {count} đầu sách (số kiểm soát {(last + 1):D3} - {(last + count):D3}).";
            return RedirectToAction(nameof(Index), new { bookId = book.Id });
        }

        // sửa đầu sách (update): tình trạng + ghi chú
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var copy = await _db.BookCopies.AsNoTracking().Include(c => c.Book).FirstOrDefaultAsync(c => c.Id == id);
            if (copy == null) return NotFound();

            if (copy.Status == BookStatus.Disposed)
            {
                TempData["Err"] = "Đầu sách đã thanh lý, không thể sửa.";
                return RedirectToAction(nameof(Index), new { bookId = copy.BookId });
            }
            if (copy.Status == BookStatus.Borrowed)
            {
                TempData["Err"] = "Đầu sách đang cho mượn, không thể sửa. Hãy xử lý trả sách trước.";
                return RedirectToAction(nameof(Index), new { bookId = copy.BookId });
            }

            return View(new CopyEditViewModel
            {
                Id = copy.Id,
                Status = copy.Status,
                Note = copy.Note,
                BookId = copy.BookId,
                BookTitle = copy.Book?.Title,
                ControlText = copy.ControlText
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CopyEditViewModel vm)
        {
            var copy = await _db.BookCopies.Include(c => c.Book).FirstOrDefaultAsync(c => c.Id == vm.Id);
            if (copy == null) return NotFound();
            if (copy.Status == BookStatus.Borrowed || copy.Status == BookStatus.Disposed)
            {
                TempData["Err"] = "Đầu sách đang cho mượn hoặc đã thanh lý, không thể sửa.";
                return RedirectToAction(nameof(Index), new { bookId = copy.BookId });
            }

            // Admin chỉ được chọn Còn / Hỏng / Mất. "Cho mượn" do hệ thống tự đặt khi có người mượn.
            if (vm.Status is { } st && st != BookStatus.Available && st != BookStatus.Damaged && st != BookStatus.Lost)
                ModelState.AddModelError(nameof(vm.Status), "Tình trạng không hợp lệ");

            if (!ModelState.IsValid)
            {
                vm.BookId = copy.BookId;
                vm.BookTitle = copy.Book?.Title;
                vm.ControlText = copy.ControlText;
                return View(vm);
            }

            var newStatus = vm.Status!.Value;
            var newNote = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
            if (newStatus != copy.Status)
            {
                Logs.Copy(_db, copy, copy.Status, newStatus, "Admin cập nhật tình trạng", User.Identity?.Name, null, newNote);
                copy.Status = newStatus;
                copy.StatusChangedAt = DateTime.Now;
            }
            else if (newNote != copy.Note)
                Logs.Copy(_db, copy, copy.Status, copy.Status, "Cập nhật ghi chú", User.Identity?.Name, null, newNote);
            copy.Note = newNote;
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã cập nhật đầu sách {copy.ControlText}.";
            return RedirectToAction(nameof(Index), new { bookId = copy.BookId });

        }

        // xóa đầu sách (Delete), chỉ xóa dc khi chưa từng được mượn
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var copy = await _db.BookCopies.FirstOrDefaultAsync(c => c.Id == id);
            if (copy == null) return NotFound();

            if (copy.Status == BookStatus.Borrowed || await _db.LoanItems.AnyAsync(i => i.CopyId == id)
                || await _db.DisposalItems.AnyAsync(d => d.CopyId == id))
            {
                TempData["Err"] = $"Không thể xóa đầu sách {copy.ControlText} vì đã có lịch sử mượn.";
                return RedirectToAction(nameof(Index), new { bookId = copy.BookId });
            }
            var bookId = copy.BookId;
            _db.BookCopies.Remove(copy);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã xóa đầu sách.";
            return RedirectToAction(nameof(Index), new { bookId });
        }
    }
}
