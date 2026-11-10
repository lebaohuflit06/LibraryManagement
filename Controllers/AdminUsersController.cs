using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // Quản lý tài khoản thành viên: xem / tìm, thêm, sửa, đặt lại mật khẩu, khóa - mở khóa, xóa
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminUsersController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<AppUser> _hasher = new();
        public AdminUsersController(AppDbContext db) => _db = db;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // danh sách + tìm kiếm (không cần gõ dấu)
        public async Task<IActionResult> Index(string? q)
        {
            var users = await _db.Users.AsNoTracking().Include(u => u.Card).OrderBy(u => u.Id).ToListAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                users = users.Where(u =>
                    TextHelper.Normalize(u.FullName).Contains(key) ||
                    TextHelper.Normalize(u.Email).Contains(key) ||
                    TextHelper.Normalize(u.Card?.CardNumber).Contains(key)).ToList();
            }
            ViewBag.Q = q;
            ViewBag.CurrentUserId = CurrentUserId;
            return View(users);
        }

        // thêm tài khoản
        [HttpGet]
        public IActionResult Create() => View(new AdminUserCreateViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserCreateViewModel vm)
        {
            if (vm.Role != AppRoles.Admin && vm.Role != AppRoles.Member)
                ModelState.AddModelError(nameof(vm.Role), "Quyền không hợp lệ");
            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng");
            if (!ModelState.IsValid) return View(vm);

            var user = new AppUser { FullName = vm.FullName.Trim(), Email = email, Role = vm.Role };
            user.PasswordHash = _hasher.HashPassword(user, vm.Password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã tạo tài khoản {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // sửa họ tên, email, đặt lại mật khẩu
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();
            return View(new AdminUserEditViewModel { Id = u.Id, FullName = u.FullName, Email = u.Email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminUserEditViewModel vm)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (u == null) return NotFound();

            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(x => x.Email == email && x.Id != vm.Id))
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng");
            if (!ModelState.IsValid) return View(vm);

            u.FullName = vm.FullName.Trim();
            u.Email = email;
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
                u.PasswordHash = _hasher.HashPassword(u, vm.NewPassword);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã cập nhật tài khoản {u.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // khóa / mở khóa tài khoản
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();
            if (u.Id == CurrentUserId)
            {
                TempData["Err"] = "Bạn không thể tự khóa tài khoản của mình.";
                return RedirectToAction(nameof(Index));
            }
            u.IsLocked = !u.IsLocked;
            await _db.SaveChangesAsync();
            TempData["Msg"] = u.IsLocked ? $"Đã khóa tài khoản {u.Email}." : $"Đã mở khóa tài khoản {u.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // xóa tài khoản (chỉ khi chưa có thẻ thư viện)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();
            if (u.Id == CurrentUserId)
            {
                TempData["Err"] = "Bạn không thể tự xóa tài khoản của mình.";
                return RedirectToAction(nameof(Index));
            }
            if (await _db.Cards.AnyAsync(c => c.UserId == id))
            {
                TempData["Err"] = $"Không thể xóa {u.Email} vì tài khoản đã có thẻ thư viện. Hãy khóa tài khoản thay vì xóa.";
                return RedirectToAction(nameof(Index));
            }
            if (u.Role == AppRoles.Admin && await _db.Users.CountAsync(x => x.Role == AppRoles.Admin) <= 1)
            {
                TempData["Err"] = "Không thể xóa quản trị viên cuối cùng.";
                return RedirectToAction(nameof(Index));
            }
            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            TempData["Msg"] = $"Đã xóa tài khoản {u.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
