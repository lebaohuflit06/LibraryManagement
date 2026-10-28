using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


namespace LibraryManagement.Controllers
{
    [Authorize]
    public class LoansController : Controller
    {
        private readonly AppDbContext _db;
        private readonly FeeService _fees;
        public LoansController(AppDbContext db, FeeService fees) { _db = db; _fees = fees; }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsStaff => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Librarian);


        // phiếu mượn hiện tại + lịch sử
        [Authorize(Roles = AppRoles.Member)]
        public async Task<IActionResult> Index()
        {
            var uid = UserId;
            var card = await _db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == uid);
            if (card == null) return RedirectToAction("Register", "LibraryCard");

            var loans = await _db.Loans.AsNoTracking()
                .Include(l => l.Items).ThenInclude(i => i.Copy).ThenInclude(c => c!.Book)
                .Where(l => l.CardId == card.Id)
                .OrderByDescending(l => l.Id)
                .ToListAsync();

            var unpaid = await _db.Fees.Where(f => f.CardId == card.Id && f.Status == FeeStatus.Unpaid).SumAsync(f => (int?)f.Amount) ?? 0;
            return View(new MyLoansViewModel { Card = card, Loans = loans, UnpaidFees = unpaid });
        }

        // phí của tôi: bồi thường, trả muộn... và tình trạng thanh toán
        [Authorize(Roles = AppRoles.Member)]
        public async Task<IActionResult> Fees()
        {
            var uid = UserId;
            var card = await _db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == uid);
            if (card == null) return RedirectToAction("Register", "LibraryCard");

            var fees = await _db.Fees.AsNoTracking().Where(f => f.CardId == card.Id).OrderByDescending(f => f.Id).ToListAsync();
            return View(new MyFeesViewModel
            {
                Card = card,
                Fees = fees,
                UnpaidAmount = fees.Where(f => f.IsUnpaid).Sum(f => f.Amount)
            });
        }


        // đăng ký mượn sách
        [HttpGet, Authorize(Roles = AppRoles.Member)]
        public async Task<IActionResult> Create(int? bookId)
        {
            var uid = UserId;
            var card = await _db.Cards.FirstOrDefaultAsync(c => c.UserId == uid);
            if (card == null) return RedirectToAction("Register", "LibraryCard");

            var reason = await BorrowBlockReason(card);
            if (reason != null)
            {
                TempData["Err"] = reason;
                return RedirectToAction(nameof(Index));
            }

            var vm = new BorrowViewModel { Book1 = bookId };
            await LoadBooks(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Member)]
        public async Task<IActionResult> Create(BorrowViewModel vm)
        {
            var uid = UserId;
            var card = await _db.Cards.FirstOrDefaultAsync(c => c.UserId == uid);
            if (card == null) return RedirectToAction("Register", "LibraryCard");

            var reason = await BorrowBlockReason(card);
            if (reason != null)
            {
                TempData["Err"] = reason;
                return RedirectToAction(nameof(Index));
            }

            // kiểm tra trùng mã sách đã chọn
            var bookIds = new[] { vm.Book1, vm.Book2, vm.Book3 }
            .Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (bookIds.Count != bookIds.Distinct().Count())
                ModelState.AddModelError("", "Không được chọn trùng sách");

            // với mỗi mã sách, lấy 1 đầu sách đang còn (số kiểm soát nhỏ nhất)
            var copies = new List<BookCopy>();
            if (ModelState.IsValid)
            {
                foreach (var id in bookIds)
                {
                    var copy = await _db.BookCopies.Include(c => c.Book)
                        .Where(c => c.BookId == id && c.Status == BookStatus.Available)
                        .OrderBy(c => c.ControlNo)
                        .FirstOrDefaultAsync();
                    if (copy == null)
                    {
                        ModelState.AddModelError("", "Một trong cuốn sách được chọn đã hết, vui lòng chọn cuốn khác");
                        break;
                    }
                    copies.Add(copy);
                }
            }
            if (!ModelState.IsValid)
            {
                await LoadBooks(vm);
                return View(vm);
            }

            var now = DateTime.Now;
            var loan = new Loan
            {
                CardId = card.Id,
                BorrowedAt = now,
                DueDate = DateTime.Today.AddDays(LibraryRules.LoanDays)
            };

            foreach (var copy in copies)
            {
                loan.Items.Add(new LoanItem { Copy = copy });
                copy.Status = BookStatus.Borrowed; // cập nhật tình trạng available -> borrowed
                copy.StatusChangedAt = now;
            }

            // lưu phiếu mượn trước để có Id, rồi ghi nhật ký từng cuốn (cùng 1 transaction)
            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                _db.Loans.Add(loan);
                await _db.SaveChangesAsync();
                foreach (var copy in copies)
                    Logs.Copy(_db, copy, BookStatus.Available, BookStatus.Borrowed, "Cho mượn", User.Identity?.Name, loan.Id, $"Thẻ {card.CardNumber}");
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }

            TempData["Msg"] = $"Mượn thành công {copies.Count} cuốn. Hạn trả: {loan.DueDate:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        // trả sách (độc giả tự trả hoặc Admin / Thủ thư xử lý tại quầy)
        [HttpGet]
        public async Task<IActionResult> Return (int id)
        {
            var loan = await FindActiveLoan(id);
            if (loan == null) return NotFound();

            var vm = new ReturnViewModel { LoanId = loan.Id, Loan = loan };
            foreach (var item in loan.Items.OrderBy(i => i.Id))
                vm.Items.Add(new ReturnItemInput { LoanItemId = item.Id, Condition = BookStatus.Available });
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Return (ReturnViewModel vm)
        {
            var loan = await FindActiveLoan(vm.LoanId);
            if (loan == null) return NotFound();

            // danh sách gửi phải khớp đúng các cuốn của phiếu
            var loanIds = loan.Items.Select(i => i.Id).OrderBy(x => x).ToList();
            var postedIds = vm.Items.Select(i => i.LoanItemId).OrderBy(x => x).ToList();
            if (!loanIds.SequenceEqual(postedIds)) return BadRequest();

            // chỉ được chọn còn/hỏng/mất
            for (var i = 0; i < vm.Items.Count; i++)
            {
                if (vm.Items[i].Condition is { } cd &&
                    cd != BookStatus.Available && cd != BookStatus.Damaged && cd != BookStatus.Lost)
                    ModelState.AddModelError($"Items[{i}].Condition", "Tình trạng không hợp lệ");
            }
            if (!ModelState.IsValid)
            {
                vm.Loan = loan;
                return View(vm);
            }
            var now = DateTime.Now;
            var actor = User.Identity?.Name;
            var anyCompensation = false;
            foreach (var input in vm.Items)
            {
                var item = loan.Items.First(x => x.Id == input.LoanItemId);
                var status = input.Condition!.Value;

                item.ReturnStatus = status;
                item.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
                item.Compensation = LibraryRules.CalculateCompensation(status, item.Copy!.Book!.Price);
                item.CompensationPaid = false;

                var before = item.Copy.Status;
                item.Copy.Status = status; //cập nhật tình trạng đầu sách
                item.Copy.StatusChangedAt = now;
                Logs.Copy(_db, item.Copy, before, status, "Trả sách", actor, loan.Id, item.Note);

                if (item.Compensation > 0) anyCompensation = true;
            }

            loan.ReturnedAt = now;
            if (anyCompensation)
                loan.Card!.IsActive = false; // mất / hỏng sách thì thẻ sẽ không dc sử dựng đến khi bồi thường

            // ghi sổ phí: bồi thường mất/hỏng + phí trả muộn (nếu có)
            var fees = _fees.CreateReturnFees(loan, now);

            await _db.SaveChangesAsync();

            var late = fees.FirstOrDefault(f => f.Type == FeeType.Late);
            if (anyCompensation || late != null)
            {
                var msg = "Đã ghi nhận trả sách.";
                if (anyCompensation)
                    msg += $" Có sách mất/hỏng: cần bồi thường {LibraryRules.Money(loan.TotalCompensation)} để thẻ sử dụng lại được.";
                if (late != null)
                    msg += $" Phí trả muộn {LibraryRules.LateDays(loan.DueDate, now)} ngày: {LibraryRules.Money(late.Amount)}.";
                TempData["Err"] = msg;
            }
            else
                TempData["Msg"] = "Trả sách thành công.";

            // admin / thủ thư quay vể trang chi tiết thẻ, member quay về "sách của tôi"
            if (IsStaff) return RedirectToAction("Details", "AdminCards", new { id = loan.CardId });
            return RedirectToAction(nameof(Index));
        }

        // trả về lý do không được mượn
        private async Task<string?> BorrowBlockReason(LibraryCard card)
        {
            if (!card.IsActive)
                return "Thẻ thư viện không sử dụng được.";
            if (card.ExpiresAt < DateTime.Now)
                return "Thẻ thư viện hết hạn.";
            var unpaid = await _db.Fees.Where(f => f.CardId == card.Id && f.Status == FeeStatus.Unpaid).SumAsync(f => (int?)f.Amount) ?? 0;
            if (unpaid > 0)
                return $"Bạn còn khoản phí chưa thanh toán: {LibraryRules.Money(unpaid)}. Vui lòng thanh toán tại thư viện trước khi mượn tiếp.";
            if (await _db.Loans.AnyAsync(l => l.CardId == card.Id && l.ReturnedAt == null))
                return "Bạn đang mượn sách, hãy trả sách xong mới được mượn tiếp!";
            return null;
        }

        // dropdown, chỉ mã sách có ít nhất 1 đầu sách Available
        private async Task LoadBooks(BorrowViewModel vm)
        {
            vm.Books = await _db.Books.AsNoTracking()
                .Where(b => b.Copies.Any(c => c.Status == BookStatus.Available))
                .OrderBy(b => b.Title)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Title + " - " + b.Author })
                .ToListAsync();
        }

        // phiếu chưa trả, member chỉ thấy phiếu của mình, admin / thủ thư thấy tất cả
        private async Task<Loan?> FindActiveLoan(int loanId)
        {
            var uid = UserId;
            var isStaff = IsStaff;
            return await _db.Loans
                .Include(l => l.Card)
                .Include(l => l.Items).ThenInclude(i => i.Copy).ThenInclude(c => c!.Book)
                .FirstOrDefaultAsync(l => l.Id == loanId && l.ReturnedAt == null && (isStaff || l.Card!.UserId == uid));
        }
    }
}
