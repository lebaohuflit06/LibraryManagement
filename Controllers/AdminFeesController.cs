using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // xử lý các khoản phí của độc giả: bồi thường mất/hỏng, phí trả muộn, phí khác.
    // Thu tiền + in phiếu thu: Admin và Thủ thư. Miễn phí: chỉ Admin.
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminFeesController : Controller
    {
        private const int PageSize = 15;
        private readonly AppDbContext _db;
        private readonly FeeService _fees;
        public AdminFeesController(AppDbContext db, FeeService fees) { _db = db; _fees = fees; }

        private string Actor => User.Identity?.Name ?? "";

        // sổ phí: lọc theo tình trạng / loại, tìm kiếm (read)
        public async Task<IActionResult> Index(string? status = "unpaid", string? type = null, string? q = null, int page = 1)
        {
            var all = await _db.Fees.AsNoTracking()
                .Include(f => f.Card).ThenInclude(c => c!.User)
                .OrderByDescending(f => f.Id)
                .ToListAsync();

            var now = DateTime.Now;
            var today = DateTime.Today;
            var vm = new FeeListViewModel
            {
                Status = status,
                Type = type,
                Q = q,
                UnpaidAmount = all.Where(f => f.IsUnpaid).Sum(f => f.Amount),
                UnpaidCount = all.Count(f => f.IsUnpaid),
                PaidThisMonth = all.Where(f => f.Status == FeeStatus.Paid && f.ProcessedAt != null
                    && f.ProcessedAt.Value.Year == now.Year && f.ProcessedAt.Value.Month == now.Month).Sum(f => f.Amount),
                WaivedAmount = all.Where(f => f.Status == FeeStatus.Waived).Sum(f => f.Amount),
                OverdueLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && l.DueDate < today)
            };

            var query = all.AsEnumerable();
            query = status switch
            {
                "paid" => query.Where(f => f.Status == FeeStatus.Paid),
                "waived" => query.Where(f => f.Status == FeeStatus.Waived),
                "all" => query,
                _ => query.Where(f => f.IsUnpaid)
            };
            query = type switch
            {
                "late" => query.Where(f => f.Type == FeeType.Late),
                "damaged" => query.Where(f => f.Type == FeeType.Damaged),
                "lost" => query.Where(f => f.Type == FeeType.Lost),
                "other" => query.Where(f => f.Type == FeeType.Other),
                _ => query
            };
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                query = query.Where(f =>
                    TextHelper.Normalize(f.Card?.CardNumber).Contains(key) ||
                    TextHelper.Normalize(f.Card?.User?.FullName).Contains(key) ||
                    TextHelper.Normalize(f.Card?.User?.Email).Contains(key) ||
                    TextHelper.Normalize(f.ReceiptNo).Contains(key) ||
                    TextHelper.Normalize(f.Description).Contains(key));
            }
            var list = query.ToList();

            vm.Total = list.Count;
            vm.TotalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)PageSize));
            vm.Page = Math.Clamp(page, 1, vm.TotalPages);
            vm.Fees = list.Skip((vm.Page - 1) * PageSize).Take(PageSize).ToList();
            return View(vm);
        }

        // sách đang mượn đã quá hạn + phí trả muộn đang tích lũy (chưa ghi sổ, sẽ ghi khi trả sách)
        public async Task<IActionResult> Overdue()
        {
            var today = DateTime.Today;
            var loans = await _db.Loans.AsNoTracking()
                .Include(l => l.Card).ThenInclude(c => c!.User)
                .Include(l => l.Items).ThenInclude(i => i.Copy).ThenInclude(c => c!.Book)
                .Where(l => l.ReturnedAt == null && l.DueDate < today)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            var now = DateTime.Now;
            var rows = loans.Select(l => new OverdueRow
            {
                Loan = l,
                Days = LibraryRules.LateDays(l.DueDate, now),
                Accrued = FeeService.AccruedLateFee(l, now)
            }).ToList();
            return View(new OverdueViewModel { Rows = rows, TotalAccrued = rows.Sum(r => r.Accrued) });
        }

        // thu tiền (một khoản, hoặc kèm các khoản khác của cùng thẻ)
        [HttpGet]
        public async Task<IActionResult> Pay(int id, bool all = false)
        {
            var fee = await LoadFee(id);
            if (fee == null) return NotFound();
            if (!fee.IsUnpaid)
            {
                TempData["Err"] = "Khoản phí này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }
            var vm = new FeePayViewModel { Id = id, IncludeOthers = all };
            await FillPay(vm, fee);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(FeePayViewModel vm)
        {
            var fee = await LoadFee(vm.Id);
            if (fee == null) return NotFound();
            if (!fee.IsUnpaid)
            {
                TempData["Err"] = "Khoản phí này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                await FillPay(vm, fee);
                return View(vm);
            }

            var toPay = new List<Fee> { fee };
            if (vm.IncludeOthers)
                toPay.AddRange(await _db.Fees
                    .Where(f => f.CardId == fee.CardId && f.Status == FeeStatus.Unpaid && f.Id != fee.Id).ToListAsync());

            var total = toPay.Sum(f => f.Amount);
            var (receiptNo, result) = await _fees.PayAsync(toPay, vm.Method!.Value, vm.Note, Actor);

            TempData["Msg"] = $"Đã thu {LibraryRules.Money(total)} ({toPay.Count} khoản) — phiếu thu {receiptNo}." +
                (result.CardReactivated ? " Thẻ đã SỬ DỤNG ĐƯỢC trở lại." : "") +
                (result.CardStillBlocked ? " Thẻ vẫn còn khoản bồi thường chưa thanh toán nên chưa dùng được." : "");
            return RedirectToAction(nameof(Receipt), new { no = receiptNo });
        }

        // miễn phí (chỉ Admin)
        [HttpGet, Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Waive(int id)
        {
            var fee = await LoadFee(id);
            if (fee == null) return NotFound();
            if (!fee.IsUnpaid)
            {
                TempData["Err"] = "Khoản phí này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }
            return View(new FeeWaiveViewModel { Id = id, Fee = fee });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Waive(FeeWaiveViewModel vm)
        {
            var fee = await LoadFee(vm.Id);
            if (fee == null) return NotFound();
            if (!fee.IsUnpaid)
            {
                TempData["Err"] = "Khoản phí này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                vm.Fee = fee;
                return View(vm);
            }

            var result = await _fees.WaiveAsync(fee, vm.Reason, Actor);
            Logs.Audit(_db, Actor, "Miễn phí", $"Thẻ {fee.Card?.CardNumber} - {fee.Card?.User?.FullName}",
                $"{Labels.FeeType(fee.Type)} {LibraryRules.Money(fee.Amount)}: {vm.Reason.Trim()}");
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã miễn khoản phí {LibraryRules.Money(fee.Amount)}." +
                (result.CardReactivated ? " Thẻ đã SỬ DỤNG ĐƯỢC trở lại." : "");
            return RedirectToAction(nameof(Index));
        }

        // phiếu thu (xem + in)
        public async Task<IActionResult> Receipt(string no)
        {
            var fees = await _db.Fees.AsNoTracking()
                .Include(f => f.Card).ThenInclude(c => c!.User)
                .Where(f => f.ReceiptNo == no && f.Status == FeeStatus.Paid)
                .OrderBy(f => f.Id).ToListAsync();
            if (fees.Count == 0) return NotFound();
            return View(new ReceiptViewModel { ReceiptNo = no, Fees = fees, Card = fees[0].Card!, Total = fees.Sum(f => f.Amount) });
        }

        // thu thêm khoản phí khác (ví dụ: làm lại thẻ, phí dịch vụ)
        [HttpGet]
        public async Task<IActionResult> Create(int? cardId)
        {
            var vm = new FeeCreateViewModel { CardId = cardId };
            await LoadCards(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeeCreateViewModel vm)
        {
            if (vm.CardId is { } cid && !await _db.Cards.AnyAsync(c => c.Id == cid))
                ModelState.AddModelError(nameof(vm.CardId), "Thẻ không tồn tại");
            if (!ModelState.IsValid)
            {
                await LoadCards(vm);
                return View(vm);
            }

            var fee = new Fee
            {
                CardId = vm.CardId!.Value,
                Type = FeeType.Other,
                Amount = vm.Amount!.Value,
                Description = vm.Description.Trim(),
                CreatedAt = DateTime.Now
            };
            _db.Fees.Add(fee);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã ghi nhận khoản phí {LibraryRules.Money(fee.Amount)}. Hãy thu tiền để hoàn tất.";
            return RedirectToAction(nameof(Pay), new { id = fee.Id });
        }

        private Task<Fee?> LoadFee(int id) =>
            _db.Fees.Include(f => f.Card).ThenInclude(c => c!.User).FirstOrDefaultAsync(f => f.Id == id);

        private async Task FillPay(FeePayViewModel vm, Fee fee)
        {
            vm.Fee = fee;
            vm.Others = await _db.Fees.AsNoTracking()
                .Where(f => f.CardId == fee.CardId && f.Status == FeeStatus.Unpaid && f.Id != fee.Id)
                .OrderBy(f => f.Id).ToListAsync();
        }

        private async Task LoadCards(FeeCreateViewModel vm)
        {
            vm.Cards = await _db.Cards.AsNoTracking().Include(c => c.User)
                .OrderBy(c => c.CardNumber)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CardNumber + " - " + c.User!.FullName })
                .ToListAsync();
        }
    }
}
