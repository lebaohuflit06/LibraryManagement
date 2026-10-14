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
    }
}
