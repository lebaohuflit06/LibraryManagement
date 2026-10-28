using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // theo dõi tình trạng sách: tổng quan toàn bộ kho, lọc theo tình trạng, xem lịch sử từng cuốn
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminTrackingController : Controller
    {
        private const int PageSize = 20;
        private readonly AppDbContext _db;
        public AdminTrackingController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? q, string? status, string? category, int page = 1)
        {
            var copies = await _db.BookCopies.AsNoTracking().Include(c => c.Book)
                .OrderBy(c => c.Book!.Title).ThenBy(c => c.ControlNo).ToListAsync();

            var loanCounts = await _db.LoanItems.AsNoTracking().GroupBy(i => i.CopyId)
                .Select(g => new { CopyId = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.CopyId, x => x.N);

            // phiếu đang mượn của từng cuốn
            var openItems = await _db.LoanItems.AsNoTracking()
                .Include(i => i.Loan).ThenInclude(l => l!.Card).ThenInclude(c => c!.User)
                .Where(i => i.Loan!.ReturnedAt == null).ToListAsync();
            var openByCopy = openItems.GroupBy(i => i.CopyId).ToDictionary(g => g.Key, g => g.First().Loan!);

            var rows = copies.Select(c => new TrackingRow
            {
                Copy = c,
                CurrentLoan = c.Status == BookStatus.Borrowed && openByCopy.TryGetValue(c.Id, out var l) ? l : null,
                LoanCount = loanCounts.TryGetValue(c.Id, out var n) ? n : 0
            }).ToList();

            var vm = new TrackingViewModel
            {
                Q = q,
                Status = status,
                Category = category,
                Categories = copies.Select(c => c.Book!.Category).Distinct().OrderBy(x => x).ToList(),
                All = rows.Count,
                Available = rows.Count(r => r.Copy.Status == BookStatus.Available),
                Borrowed = rows.Count(r => r.Copy.Status == BookStatus.Borrowed),
                Overdue = rows.Count(r => r.IsOverdue),
                Damaged = rows.Count(r => r.Copy.Status == BookStatus.Damaged),
                Lost = rows.Count(r => r.Copy.Status == BookStatus.Lost),
                Disposed = rows.Count(r => r.Copy.Status == BookStatus.Disposed)
            };

            var query = rows.AsEnumerable();
            query = status switch
            {
                "available" => query.Where(r => r.Copy.Status == BookStatus.Available),
                "borrowed" => query.Where(r => r.Copy.Status == BookStatus.Borrowed),
                "overdue" => query.Where(r => r.IsOverdue),
                "damaged" => query.Where(r => r.Copy.Status == BookStatus.Damaged),
                "lost" => query.Where(r => r.Copy.Status == BookStatus.Lost),
                "disposed" => query.Where(r => r.Copy.Status == BookStatus.Disposed),
                _ => query
            };
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(r => r.Copy.Book!.Category == category);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                query = query.Where(r =>
                    TextHelper.Normalize(r.Copy.Book!.Title).Contains(key) ||
                    TextHelper.Normalize(r.Copy.Book!.Author).Contains(key) ||
                    TextHelper.Normalize(r.CurrentLoan?.Card?.User?.FullName).Contains(key) ||
                    TextHelper.Normalize(r.CurrentLoan?.Card?.CardNumber).Contains(key) ||
                    r.Copy.ControlText == q.Trim());   // gõ số kiểm soát, ví dụ 003
            }
            var list = query.ToList();

            vm.TotalRows = list.Count;
            vm.TotalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)PageSize));
            vm.Page = Math.Clamp(page, 1, vm.TotalPages);
            vm.Rows = list.Skip((vm.Page - 1) * PageSize).Take(PageSize).ToList();
            return View(vm);
        }

        // lịch sử tình trạng + các lượt mượn của một cuốn
        public async Task<IActionResult> Copy(int id)
        {
            var copy = await _db.BookCopies.AsNoTracking().Include(c => c.Book).FirstOrDefaultAsync(c => c.Id == id);
            if (copy == null) return NotFound();

            var logs = await _db.CopyLogs.AsNoTracking().Where(l => l.CopyId == id)
                .OrderByDescending(l => l.At).ThenByDescending(l => l.Id).ToListAsync();
            var loans = await _db.LoanItems.AsNoTracking()
                .Include(i => i.Loan).ThenInclude(l => l!.Card).ThenInclude(c => c!.User)
                .Where(i => i.CopyId == id).OrderByDescending(i => i.Id).ToListAsync();
            var disposal = await _db.DisposalItems.AsNoTracking().Include(d => d.Report)
                .FirstOrDefaultAsync(d => d.CopyId == id);

            return View(new CopyHistoryViewModel { Copy = copy, Logs = logs, Loans = loans, Disposal = disposal });
        }
    }
}
