using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // Xử lý các khoản phí của người dùng (tiền bồi thường sách mất / hỏng)
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminFeesController : Controller
    {
        private readonly AppDbContext _db;
        public AdminFeesController(AppDbContext db) => _db = db;

        // filter: unpaid (chưa thu) | paid (đã thu) | all
        public async Task<IActionResult> Index(string? filter, string? q)
        {
            filter = filter is "paid" or "all" ? filter : "unpaid";

            var all = await _db.LoanItems.AsNoTracking()
                .Include(i => i.Loan).ThenInclude(l => l!.Card).ThenInclude(c => c!.User)
                .Include(i => i.Copy).ThenInclude(c => c!.Book)
                .Where(i => i.Compensation > 0)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            var vm = new FeesViewModel
            {
                Filter = filter,
                Q = q,
                TotalUnpaid = all.Where(i => !i.CompensationPaid).Sum(i => i.Compensation),
                TotalPaid = all.Where(i => i.CompensationPaid).Sum(i => i.Compensation)
            };

            IEnumerable<LoanItem> items = all;
            if (filter == "unpaid") items = items.Where(i => !i.CompensationPaid);
            else if (filter == "paid") items = items.Where(i => i.CompensationPaid);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                items = items.Where(i =>
                    TextHelper.Normalize(i.Loan?.Card?.User?.FullName).Contains(key) ||
                    TextHelper.Normalize(i.Loan?.Card?.CardNumber).Contains(key) ||
                    TextHelper.Normalize(i.Copy?.Book?.Title).Contains(key));
            }
            vm.Items = items.ToList();
            return View(vm);
        }

        // xác nhận đã thu một khoản bồi thường; hết nợ thì thẻ dùng lại được
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id, string? filter)
        {
            var item = await _db.LoanItems.Include(i => i.Loan).ThenInclude(l => l!.Card)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            if (item.IsUnpaid)
            {
                var card = item.Loan!.Card!;
                var cardId = card.Id;
                var otherUnpaid = await _db.LoanItems.AnyAsync(i =>
                    i.Id != id && i.Loan!.CardId == cardId && i.Compensation > 0 && !i.CompensationPaid);

                item.CompensationPaid = true;
                if (!otherUnpaid) card.IsActive = true;
                await _db.SaveChangesAsync();

                TempData["Msg"] = otherUnpaid
                    ? "Đã xác nhận thu tiền. Thẻ vẫn còn khoản bồi thường chưa thanh toán."
                    : $"Đã xác nhận thu tiền. Thẻ {card.CardNumber} đã sử dụng được trở lại.";
            }
            return RedirectToAction(nameof(Index), new { filter });
        }
    }
}
