using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // Theo dõi tình trạng sách: từng cuốn đang ở trạng thái nào, ai đang mượn, phiếu nào quá hạn
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminTrackingController : Controller
    {
        private readonly AppDbContext _db;
        public AdminTrackingController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(BookStatus? status, string? q)
        {
            var copies = await _db.BookCopies.AsNoTracking().Include(c => c.Book).ToListAsync();

            var vm = new TrackingViewModel
            {
                Q = q,
                Status = status,
                AvailableCount = copies.Count(c => c.Status == BookStatus.Available),
                BorrowedCount = copies.Count(c => c.Status == BookStatus.Borrowed),
                LostCount = copies.Count(c => c.Status == BookStatus.Lost),
                DamagedCount = copies.Count(c => c.Status == BookStatus.Damaged),
                LiquidatedCount = copies.Count(c => c.Status == BookStatus.Liquidated)
            };

            IEnumerable<BookCopy> filtered = copies;
            if (status.HasValue) filtered = filtered.Where(c => c.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                filtered = filtered.Where(c =>
                    TextHelper.Normalize(c.Book?.Title).Contains(key) ||
                    TextHelper.Normalize(c.Book?.Author).Contains(key) ||
                    c.ControlText.Contains(key));
            }
            var list = filtered.OrderBy(c => c.Book?.Title).ThenBy(c => c.ControlNo).ToList();

            // phiếu đang giữ từng cuốn đang cho mượn
            var borrowedIds = list.Where(c => c.Status == BookStatus.Borrowed).Select(c => c.Id).ToList();
            var activeItems = await _db.LoanItems.AsNoTracking()
                .Include(i => i.Loan).ThenInclude(l => l!.Card).ThenInclude(c => c!.User)
                .Where(i => borrowedIds.Contains(i.CopyId) && i.Loan!.ReturnedAt == null)
                .ToListAsync();

            foreach (var c in list)
            {
                var item = activeItems.FirstOrDefault(i => i.CopyId == c.Id);
                vm.Rows.Add(new TrackingRow { Copy = c, ActiveLoan = item?.Loan });
            }

            var today = DateTime.Today;
            vm.OverdueLoans = await _db.Loans.AsNoTracking()
                .Include(l => l.Card).ThenInclude(c => c!.User)
                .Include(l => l.Items).ThenInclude(i => i.Copy).ThenInclude(c => c!.Book)
                .Where(l => l.ReturnedAt == null && l.DueDate < today)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            return View(vm);
        }
    }
}
