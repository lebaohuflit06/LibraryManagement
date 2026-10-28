using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminCardsController : Controller
    {
        private readonly AppDbContext _db;
        public AdminCardsController(AppDbContext db) => _db = db;

        // danh sách thẻ và tìm kiếm (read)
        public async Task<IActionResult> Index(string? q)
        {
            var cards = await _db.Cards.AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Loans)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                cards = cards.Where(c =>
                TextHelper.Normalize(c.User?.FullName).Contains(key) ||
                TextHelper.Normalize(c.User?.Email).Contains(key) ||
                TextHelper.Normalize(c.CardNumber).Contains(key) ||
                TextHelper.Normalize(c.IdNumber).Contains(key) ||
                TextHelper.Normalize(c.Phone).Contains(key)).ToList();
            }
            ViewBag.Q = q;
            return View(cards);
        }

        // chi tiết thẻ + lịch sử mượn, phí, bồi thường (Read)
        public async Task<IActionResult> Details(int id)
        {
            var card = await _db.Cards.AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Loans).ThenInclude(l => l.Items).ThenInclude(i => i.Copy).ThenInclude(cp => cp!.Book)
                .Include(c => c.Fees)
                .FirstOrDefaultAsync(c => c.Id == id);
            return card == null ? NotFound() : View(card);
        }

        // cấp thẻ cho thành viên (create)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new AdminCardCreateViewModel();
            await LoadUsers(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCardCreateViewModel vm)
        {
            if (vm.UserId is { } uid)
            {
                if (!await _db.Users.AnyAsync(u => u.Id == uid && u.Role == AppRoles.Member))
                    ModelState.AddModelError(nameof(vm.UserId), "Thành viên không hợp lệ");
                else if (await _db.Cards.AnyAsync(c => c.UserId == uid))
                    ModelState.AddModelError(nameof(vm.UserId), "Thành viên này đã có thẻ");
            }

            await ValidateCardFields(vm, 0);

            if (!ModelState.IsValid)
            {
                await LoadUsers(vm);
                return View(vm);
            }

            var card = new LibraryCard
            {
                UserId = vm.UserId!.Value,
                Phone = vm.Phone,
                Address = vm.Address.Trim(),
                IdNumber = vm.IdNumber,
                DateOfBirth = vm.DateOfBirth!.Value,
                IssuedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddYears(1),
                CardNumber = "TMP"
            };
            _db.Cards.Add(card);
            await _db.SaveChangesAsync();                          // lưu trước để có Id
            card.CardNumber = LibraryRules.MakeCardNumber(card.Id); // rồi mới tạo số thẻ từ Id
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã cấp thẻ {card.CardNumber}.";
            return RedirectToAction(nameof(Index));
        }

        // sửa thẻ (update)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var card = await _db.Cards.AsNoTracking().Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (card == null) return NotFound();

            return View(new AdminCardEditViewModel
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                UserName = card.User?.FullName,
                Phone = card.Phone,
                Address = card.Address,
                IdNumber = card.IdNumber,
                DateOfBirth = card.DateOfBirth,
                ExpiresAt = card.ExpiresAt,
                IsActive = card.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCardEditViewModel vm)
        {
            var card = await _db.Cards.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == vm.Id);
            if (card == null) return NotFound();

            await ValidateCardFields(vm, card.Id);
            if (vm.ExpiresAt is { } exp && exp.Date <= card.IssuedAt.Date)
                ModelState.AddModelError(nameof(vm.ExpiresAt), "Ngày hết hạn phải sau ngày cấp thẻ");

            if (!ModelState.IsValid)
            {
                vm.CardNumber = card.CardNumber;       // các trường chỉ hiển thị phải nạp lại
                vm.UserName = card.User?.FullName;
                return View(vm);
            }
            card.Phone = vm.Phone;
            card.Address = vm.Address.Trim();
            card.IdNumber = vm.IdNumber;
            card.DateOfBirth = vm.DateOfBirth!.Value;
            card.ExpiresAt = vm.ExpiresAt!.Value;
            card.IsActive = vm.IsActive;
            await _db.SaveChangesAsync();


            TempData["Msg"] = $"Đã cập nhật thẻ {card.CardNumber}.";
            return RedirectToAction(nameof(Index));
        }

        // xóa thẻ
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == id);
            if (card == null) return NotFound();

            if (await _db.Loans.AnyAsync(l => l.CardId == id) || await _db.Fees.AnyAsync(f => f.CardId == id))
            {
                TempData["Err"] = $"Không thể xóa thẻ {card.CardNumber} vì thẻ đã có lịch sử mượn sách hoặc khoản phí.";
                return RedirectToAction(nameof(Index));
            }

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();
            TempData["Msg"] = $"Đã xóa thẻ {card.CardNumber}.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra dùng chung cho Create và Edit (ignoreCardId = 0 khi tạo mới)

        private async Task ValidateCardFields(CardRegisterViewModel vm, int ignoreCardId)
        {
            if (vm.DateOfBirth is { } dob && dob > DateTime.Today.AddYears(-6))
                ModelState.AddModelError(nameof(vm.DateOfBirth), "Độc giả phải từ 6 tuổi trở lên");
            if (!string.IsNullOrEmpty(vm.IdNumber) && await _db.Cards.AnyAsync(c => c.IdNumber == vm.IdNumber && c.Id != ignoreCardId))
                ModelState.AddModelError(nameof(vm.IdNumber), "Số CCCD này đã đăng ký thẻ");
        }
        // dropdown thành viên chưa có thẻ
        private async Task LoadUsers(AdminCardCreateViewModel vm)
        {
            vm.Users = await _db.Users.AsNoTracking()
                .Where(u => u.Role == AppRoles.Member && u.Card == null)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.FullName + " (" + u.Email + ")" })
                .ToListAsync();
        }
    }
}