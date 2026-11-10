using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminReportsController : Controller
    {
        private readonly AppDbContext _db;
        public AdminReportsController(AppDbContext db) => _db = db;

        // báo cáo hằng năm các đầu sách bị mất / hỏng để thanh lý và tình hình bồi thường
        public async Task<IActionResult> LostDamaged(int? year)
        {
            var y = year ?? DateTime.Now.Year;

            var copies = await _db.BookCopies.AsNoTracking()
                .Include(c => c.Book)
                .Where(c => c.Status == BookStatus.Lost || c.Status == BookStatus.Damaged)
                .ToListAsync();
            copies = copies
                .Where(c => c.StatusChangedAt != null && c.StatusChangedAt.Value.Year == y)
                .OrderBy(c => c.StatusChangedAt)
                .ToList();

            // lần mượn đã xảy ra mất/hỏng từng cuốn (biết ai đền, đền bao nhiêu)
            var ids = copies.Select(c => c.Id).ToList();
            var items = await _db.LoanItems.AsNoTracking()
                .Include(i => i.Loan).ThenInclude(l => l!.Card).ThenInclude(c => c!.User)
                .Where(i => ids.Contains(i.CopyId) &&
                            (i.ReturnStatus == BookStatus.Lost || i.ReturnStatus == BookStatus.Damaged)).ToListAsync();

            var vm = new LostDamagedReportViewModel { Year = y };
            foreach (var c in copies)
            {
                var item = items.Where(i => i.CopyId == c.Id).OrderByDescending(i => i.Id).FirstOrDefault();
                vm.Rows.Add(new LostDamagedRow { Copy = c, Item = item });
            }
            vm.LostCount = copies.Count(c => c.Status == BookStatus.Lost);
            vm.DamagedCount = copies.Count(c => c.Status == BookStatus.Damaged);
            vm.TotalCompensation = vm.Rows.Sum(r => r.Item?.Compensation ?? 0);
            vm.UnpaidCompensation = vm.Rows.Where(r => r.Item != null && r.Item.IsUnpaid).Sum(r => r.Item!.Compensation);
            return View(vm);

        }

        // báo cáo thanh lý: sách mất / hỏng (cũ, rách) chờ thanh lý + sách đã thanh lý trong năm
        public async Task<IActionResult> Liquidation(int? year)
        {
            var y = year ?? DateTime.Now.Year;

            var copies = await _db.BookCopies.AsNoTracking().Include(c => c.Book)
                .Where(c => c.Status == BookStatus.Lost || c.Status == BookStatus.Damaged || c.Status == BookStatus.Liquidated)
                .ToListAsync();

            var ids = copies.Select(c => c.Id).ToList();
            var items = await _db.LoanItems.AsNoTracking()
                .Where(i => ids.Contains(i.CopyId) && (i.ReturnStatus == BookStatus.Lost || i.ReturnStatus == BookStatus.Damaged))
                .ToListAsync();

            LiquidationRow Row(BookCopy c) => new LiquidationRow
            {
                Copy = c,
                Item = items.Where(i => i.CopyId == c.Id).OrderByDescending(i => i.Id).FirstOrDefault()
            };

            var vm = new LiquidationReportViewModel { Year = y };
            vm.Candidates = copies.Where(c => c.Status != BookStatus.Liquidated)
                .OrderBy(c => c.StatusChangedAt).Select(Row).ToList();
            vm.Liquidated = copies.Where(c => c.Status == BookStatus.Liquidated &&
                    c.StatusChangedAt != null && c.StatusChangedAt.Value.Year == y)
                .OrderBy(c => c.StatusChangedAt).Select(Row).ToList();
            vm.CandidateValue = vm.Candidates.Sum(r => r.Copy.Book?.Price ?? 0);
            vm.LiquidatedValue = vm.Liquidated.Sum(r => r.Copy.Book?.Price ?? 0);
            return View(vm);
        }

        // thực hiện thanh lý một cuốn (chỉ cuốn đang mất / hỏng)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Liquidate(int id, int? year)
        {
            var copy = await _db.BookCopies.Include(c => c.Book).FirstOrDefaultAsync(c => c.Id == id);
            if (copy == null) return NotFound();

            if (copy.Status != BookStatus.Lost && copy.Status != BookStatus.Damaged)
            {
                TempData["Err"] = "Chỉ thanh lý được cuốn sách đang ở tình trạng mất hoặc hỏng.";
                return RedirectToAction(nameof(Liquidation), new { year });
            }

            copy.Status = BookStatus.Liquidated;
            copy.StatusChangedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã thanh lý \"{copy.Book?.Title}\" (số kiểm soát {copy.ControlText}).";
            return RedirectToAction(nameof(Liquidation), new { year });
        }
    }
}