using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminReportsController : Controller
    {
        private readonly AppDbContext _db;
        public AdminReportsController(AppDbContext db) => _db = db;

        // báo cáo hằng năm các đầu sách bị mất / hỏng trong năm (dựa trên nhật ký tình trạng,
        // nên sách đã thanh lý sau đó vẫn nằm trong báo cáo của năm xảy ra mất / hỏng)
        public async Task<IActionResult> LostDamaged(int? year)
        {
            var y = year ?? DateTime.Now.Year;

            var logs = await _db.CopyLogs.AsNoTracking()
                .Include(l => l.Copy).ThenInclude(c => c!.Book)
                .Where(l => l.ToStatus == BookStatus.Lost || l.ToStatus == BookStatus.Damaged)
                .ToListAsync();
            // mỗi cuốn lấy lần mất / hỏng gần nhất trong năm
            var events = logs.Where(l => l.At.Year == y)
                .GroupBy(l => l.CopyId)
                .Select(g => g.OrderByDescending(l => l.At).ThenByDescending(l => l.Id).First())
                .OrderBy(l => l.At)
                .ToList();

            var ids = events.Select(e => e.CopyId).ToList();
            var items = await _db.LoanItems.AsNoTracking()
                .Include(i => i.Loan).ThenInclude(l => l!.Card).ThenInclude(c => c!.User)
                .Where(i => ids.Contains(i.CopyId) &&
                            (i.ReturnStatus == BookStatus.Lost || i.ReturnStatus == BookStatus.Damaged)).ToListAsync();

            var vm = new LostDamagedReportViewModel { Year = y };
            foreach (var e in events)
            {
                var item = items.Where(i => i.CopyId == e.CopyId).OrderByDescending(i => i.Id).FirstOrDefault();
                vm.Rows.Add(new LostDamagedRow { Copy = e.Copy!, Item = item, EventStatus = e.ToStatus, EventAt = e.At });
            }
            vm.LostCount = events.Count(e => e.ToStatus == BookStatus.Lost);
            vm.DamagedCount = events.Count(e => e.ToStatus == BookStatus.Damaged);
            vm.TotalCompensation = vm.Rows.Sum(r => r.Item?.Compensation ?? 0);
            vm.UnpaidCompensation = vm.Rows.Where(r => r.Item != null && r.Item.IsUnpaid).Sum(r => r.Item!.Compensation);
            return View(vm);
        }
    }
}
