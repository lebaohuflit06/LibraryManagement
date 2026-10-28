using System.Text;
using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // thanh lý sách cũ / rác: đề xuất -> lập biên bản -> báo cáo theo năm.
    // Xem báo cáo: Admin + Thủ thư. Lập biên bản thanh lý: chỉ Admin.
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminDisposalsController : Controller
    {
        private readonly AppDbContext _db;
        public AdminDisposalsController(AppDbContext db) => _db = db;

        private string Actor => User.Identity?.Name ?? "";

        // báo cáo thanh lý theo năm
        public async Task<IActionResult> Index(int? year)
        {
            var y = year ?? DateTime.Now.Year;
            var all = await _db.DisposalReports.AsNoTracking()
                .Include(r => r.Items).ThenInclude(i => i.Copy).ThenInclude(c => c!.Book)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();

            var reports = all.Where(r => r.CreatedAt.Year == y).ToList();
            var items = reports.SelectMany(r => r.Items).ToList();

            var years = all.Select(r => r.CreatedAt.Year).Append(DateTime.Now.Year).Distinct().OrderByDescending(x => x).ToList();
            var vm = new DisposalIndexViewModel
            {
                Year = y,
                Years = years,
                Reports = reports,
                ItemCount = items.Count,
                BookValue = items.Sum(i => i.BookValue),
                Recovered = reports.Sum(r => r.RecoveredAmount),
                ByReason = items.GroupBy(i => i.Reason).ToDictionary(g => g.Key, g => g.Count()),
                ByMethod = items.GroupBy(i => i.Method).ToDictionary(g => g.Key, g => g.Count()),
                CandidateCount = (await BuildCandidates()).Count
            };
            return View(vm);
        }

        // danh sách sách đề xuất thanh lý
        [HttpGet]
        public async Task<IActionResult> Candidates(string? reason, string? q)
        {
            var vm = new DisposalCandidatesViewModel { Reason = reason, Q = q };
            await FillCandidates(vm);
            return View(vm);
        }

        // lập biên bản thanh lý (chỉ Admin)
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Create(DisposalCandidatesViewModel vm)
        {
            var candidates = await BuildCandidates();
            var chosen = candidates.Where(c => vm.CopyIds.Contains(c.Copy.Id)).ToList();

            if (vm.CopyIds.Count > 0 && chosen.Count != vm.CopyIds.Distinct().Count())
                ModelState.AddModelError("", "Có cuốn không còn đủ điều kiện thanh lý (đang được mượn hoặc đã thanh lý). Vui lòng tải lại danh sách.");
            if (vm.Method == DisposalMethod.WriteOff && chosen.Any(c => c.Copy.Status != BookStatus.Lost))
                ModelState.AddModelError(nameof(vm.Method), "\"Ghi giảm\" chỉ dành cho sách bị mất. Hãy chọn tiêu hủy / bán / tặng cho sách còn.");
            if (vm.Method == DisposalMethod.Sell && (vm.RecoveredAmount ?? 0) <= 0 && chosen.Any(c => c.Copy.Status != BookStatus.Lost))
                ModelState.AddModelError(nameof(vm.RecoveredAmount), "Bán thanh lý: vui lòng nhập số tiền thu hồi");

            if (!ModelState.IsValid)
            {
                await FillCandidates(vm);
                return View("Candidates", vm);
            }

            var now = DateTime.Now;
            var prefix = $"TL{now:yyyy}-";
            var seq = await _db.DisposalReports.CountAsync(r => r.Code.StartsWith(prefix)) + 1;
            while (await _db.DisposalReports.AnyAsync(r => r.Code == prefix + seq.ToString("D3"))) seq++;

            var note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
            var report = new DisposalReport
            {
                Code = prefix + seq.ToString("D3"),
                CreatedAt = now,
                CreatedBy = Actor,
                Note = note,
                RecoveredAmount = vm.Method == DisposalMethod.Sell ? vm.RecoveredAmount ?? 0 : 0
            };

            foreach (var c in chosen)
            {
                var copy = await _db.BookCopies.Include(x => x.Book).FirstAsync(x => x.Id == c.Copy.Id);
                var method = copy.Status == BookStatus.Lost ? DisposalMethod.WriteOff : vm.Method!.Value;
                report.Items.Add(new DisposalItem
                {
                    Copy = copy,
                    Reason = c.Reason,
                    Method = method,
                    BookValue = copy.Book?.Price ?? 0
                });
                Logs.Copy(_db, copy, copy.Status, BookStatus.Disposed, "Thanh lý (" + Labels.DisposalMethod(method) + ")", Actor, null, report.Code);
                copy.Status = BookStatus.Disposed;
                copy.StatusChangedAt = now;
            }
            _db.DisposalReports.Add(report);
            Logs.Audit(_db, Actor, "Lập biên bản thanh lý", report.Code, $"{report.Items.Count} cuốn, giá trị {LibraryRules.Money(report.TotalBookValue)}");
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã lập biên bản {report.Code}: thanh lý {report.Items.Count} cuốn.";
            return RedirectToAction(nameof(Details), new { id = report.Id });
        }

        // chi tiết biên bản (xem + in)
        public async Task<IActionResult> Details(int id)
        {
            var report = await _db.DisposalReports.AsNoTracking()
                .Include(r => r.Items).ThenInclude(i => i.Copy).ThenInclude(c => c!.Book)
                .FirstOrDefaultAsync(r => r.Id == id);
            return report == null ? NotFound() : View(report);
        }

        // xuất CSV (mở được bằng Excel): một biên bản (id) hoặc cả năm (year)
        public async Task<IActionResult> Export(int? id, int? year)
        {
            var q = _db.DisposalItems.AsNoTracking()
                .Include(i => i.Report).Include(i => i.Copy).ThenInclude(c => c!.Book).AsQueryable();
            string name;
            if (id != null) { q = q.Where(i => i.ReportId == id); name = "thanh-ly-" + id; }
            else
            {
                var y = year ?? DateTime.Now.Year;
                var from = new DateTime(y, 1, 1);
                q = q.Where(i => i.Report!.CreatedAt >= from && i.Report.CreatedAt < from.AddYears(1));
                name = "thanh-ly-" + y;
            }
            var rows = (await q.ToListAsync()).OrderBy(i => i.Report!.CreatedAt).ThenBy(i => i.Id).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Số biên bản,Ngày,Mã sách,Tên sách,Tác giả,Số kiểm soát,Lý do,Hình thức,Giá sách (đ),Người lập");
            foreach (var i in rows)
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(i.Report!.Code), i.Report.CreatedAt.ToString("dd/MM/yyyy"), i.Copy!.BookId.ToString(),
                    Csv(i.Copy.Book?.Title), Csv(i.Copy.Book?.Author), Csv(i.Copy.ControlText),
                    Csv(Labels.DisposalReason(i.Reason)), Csv(Labels.DisposalMethod(i.Method)),
                    i.BookValue.ToString(), Csv(i.Report.CreatedBy)
                }));
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", name + ".csv");
        }

        private static string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        // các cuốn đủ điều kiện đề xuất thanh lý: mất, hỏng, mượn nhiều (cũ rách), xuất bản đã lâu (cũ)
        private async Task<List<CandidateRow>> BuildCandidates()
        {
            var copies = await _db.BookCopies.AsNoTracking().Include(c => c.Book)
                .Where(c => c.Status != BookStatus.Borrowed && c.Status != BookStatus.Disposed)
                .ToListAsync();
            var loanCounts = await _db.LoanItems.AsNoTracking().GroupBy(i => i.CopyId)
                .Select(g => new { CopyId = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.CopyId, x => x.N);

            var limitYear = DateTime.Now.Year - LibraryRules.OldBookYears;
            var rows = new List<CandidateRow>();
            foreach (var c in copies)
            {
                var n = loanCounts.TryGetValue(c.Id, out var v) ? v : 0;
                var reasons = new List<DisposalReason>();
                if (c.Status == BookStatus.Lost) reasons.Add(DisposalReason.Lost);
                if (c.Status == BookStatus.Damaged) reasons.Add(DisposalReason.Damaged);
                if (c.Status == BookStatus.Available)
                {
                    if (n >= LibraryRules.WornLoanCount) reasons.Add(DisposalReason.Worn);
                    if (c.Book != null && c.Book.Year <= limitYear) reasons.Add(DisposalReason.Old);
                }
                if (reasons.Count == 0) continue;
                rows.Add(new CandidateRow { Copy = c, Reason = reasons[0], Reasons = reasons, LoanCount = n });
            }
            return rows.OrderBy(r => r.Reason).ThenBy(r => r.Copy.Book!.Title).ThenBy(r => r.Copy.ControlNo).ToList();
        }

        private async Task FillCandidates(DisposalCandidatesViewModel vm)
        {
            var all = await BuildCandidates();
            vm.DamagedCount = all.Count(r => r.Reasons.Contains(DisposalReason.Damaged));
            vm.LostCount = all.Count(r => r.Reasons.Contains(DisposalReason.Lost));
            vm.WornCount = all.Count(r => r.Reasons.Contains(DisposalReason.Worn));
            vm.OldCount = all.Count(r => r.Reasons.Contains(DisposalReason.Old));

            var query = all.AsEnumerable();
            query = vm.Reason switch
            {
                "damaged" => query.Where(r => r.Reasons.Contains(DisposalReason.Damaged)),
                "lost" => query.Where(r => r.Reasons.Contains(DisposalReason.Lost)),
                "worn" => query.Where(r => r.Reasons.Contains(DisposalReason.Worn)),
                "old" => query.Where(r => r.Reasons.Contains(DisposalReason.Old)),
                _ => query
            };
            if (!string.IsNullOrWhiteSpace(vm.Q))
            {
                var key = TextHelper.Normalize(vm.Q);
                query = query.Where(r => TextHelper.Normalize(r.Copy.Book!.Title).Contains(key) || TextHelper.Normalize(r.Copy.Book!.Author).Contains(key));
            }
            vm.Rows = query.ToList();
        }
    }
}
