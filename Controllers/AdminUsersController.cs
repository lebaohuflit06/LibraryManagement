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
    // quản lý tài khoản: danh sách, tìm kiếm, tạo, sửa, khóa / mở khóa, đặt lại mật khẩu, xóa.
    // Admin quản lý được mọi tài khoản; Thủ thư chỉ quản lý tài khoản Thành viên. Xóa tài khoản chỉ Admin.
    [Authorize(Roles = AppRoles.Staff)]
    public class AdminUsersController : Controller
    {
        private const int PageSize = 12;
        private readonly AppDbContext _db;
        private readonly PasswordHasher<AppUser> _hasher = new();
        public AdminUsersController(AppDbContext db) => _db = db;

        private int CurrentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin => User.IsInRole(AppRoles.Admin);
        private string Actor => User.Identity?.Name ?? "";

        // Admin: mọi tài khoản. Thủ thư: chỉ tài khoản Thành viên
        private bool CanManage(AppUser target) => IsAdmin || target.Role == AppRoles.Member;
        private List<string> RolesICanCreate() => IsAdmin ? AppRoles.All.ToList() : new List<string> { AppRoles.Member };

        // danh sách + tìm kiếm + lọc (read)
        public async Task<IActionResult> Index(string? q, string? role, string? status, int page = 1)
        {
            var all = await _db.Users.AsNoTracking().Include(u => u.Card)
                .OrderBy(u => u.Role == AppRoles.Admin ? 0 : u.Role == AppRoles.Librarian ? 1 : 2)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            if (!IsAdmin) all = all.Where(u => u.Role == AppRoles.Member).ToList(); // thủ thư chỉ thấy thành viên

            var query = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
            if (status == "locked") query = query.Where(u => u.IsLocked);
            else if (status == "active") query = query.Where(u => !u.IsLocked);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                query = query.Where(u =>
                    TextHelper.Normalize(u.FullName).Contains(key) ||
                    TextHelper.Normalize(u.Email).Contains(key) ||
                    TextHelper.Normalize(u.Card?.CardNumber).Contains(key));
            }
            var list = query.ToList();

            var totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            return View(new UserListViewModel
            {
                Q = q,
                Role = role,
                Status = status,
                Page = page,
                TotalPages = totalPages,
                Total = list.Count,
                MemberCount = all.Count(u => u.Role == AppRoles.Member),
                LockedCount = all.Count(u => u.IsLocked),
                Users = list.Skip((page - 1) * PageSize).Take(PageSize).ToList()
            });
        }

        // chi tiết tài khoản (read)
        public async Task<IActionResult> Details(int id)
        {
            var user = await _db.Users.AsNoTracking().Include(u => u.Card).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();

            var vm = new UserDetailsViewModel { User = user, Card = user.Card, CanManage = true, IsSelf = user.Id == CurrentId };
            if (user.Card != null)
            {
                var cardId = user.Card.Id;
                vm.LoanCount = await _db.Loans.CountAsync(l => l.CardId == cardId);
                vm.BorrowingCount = await _db.Loans.CountAsync(l => l.CardId == cardId && l.ReturnedAt == null);
                vm.UnpaidAmount = await _db.Fees.Where(f => f.CardId == cardId && f.Status == FeeStatus.Unpaid).SumAsync(f => (int?)f.Amount) ?? 0;
            }
            var prefix = "#" + user.Id + " ";
            vm.History = await _db.AuditLogs.AsNoTracking()
                .Where(a => a.Target.StartsWith(prefix))
                .OrderByDescending(a => a.Id).Take(15).ToListAsync();
            return View(vm);
        }

        // tạo tài khoản (create)
        [HttpGet]
        public IActionResult Create() => View(new UserCreateViewModel { AllowedRoles = RolesICanCreate() });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            var allowed = RolesICanCreate();
            vm.AllowedRoles = allowed;
            if (!allowed.Contains(vm.Role)) ModelState.AddModelError(nameof(vm.Role), "Bạn không có quyền tạo tài khoản với vai trò này");

            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email) && await _db.Users.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng");
            if (!ModelState.IsValid) return View(vm);

            var user = new AppUser
            {
                FullName = vm.FullName.Trim(),
                Email = email,
                Role = vm.Role,
                MustChangePassword = vm.MustChangePassword
            };
            user.PasswordHash = _hasher.HashPassword(user, vm.Password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Logs.Audit(_db, Actor, "Tạo tài khoản", Logs.UserTarget(user), $"Vai trò: {AppRoles.Text(user.Role)}");
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã tạo tài khoản {user.Email} ({AppRoles.Text(user.Role)}).";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        // sửa họ tên / email (update)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();
            return View(new UserEditViewModel { Id = user.Id, FullName = user.FullName, Email = user.Email, Role = user.Role });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel vm)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.Id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();

            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email) && await _db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng");
            if (!ModelState.IsValid)
            {
                vm.Role = user.Role;
                return View(vm);
            }

            var changes = new List<string>();
            if (user.FullName != vm.FullName.Trim()) changes.Add($"Tên: {user.FullName} → {vm.FullName.Trim()}");
            if (user.Email != email) changes.Add($"Email: {user.Email} → {email}");
            user.FullName = vm.FullName.Trim();
            user.Email = email;
            if (changes.Count > 0) Logs.Audit(_db, Actor, "Sửa tài khoản", Logs.UserTarget(user), string.Join("; ", changes));
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Đã cập nhật tài khoản.";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        // khóa tài khoản
        [HttpGet]
        public async Task<IActionResult> Lock(int id)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();
            if (user.Id == CurrentId)
            {
                TempData["Err"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (user.IsLocked) return RedirectToAction(nameof(Details), new { id });
            return View(new UserLockViewModel { Id = user.Id, FullName = user.FullName, Email = user.Email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(UserLockViewModel vm)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.Id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();
            if (user.Id == CurrentId)
            {
                TempData["Err"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Details), new { id = user.Id });
            }
            if (!ModelState.IsValid)
            {
                vm.FullName = user.FullName;
                vm.Email = user.Email;
                return View(vm);
            }

            user.IsLocked = true;
            user.LockReason = vm.Reason.Trim();
            Logs.Audit(_db, Actor, "Khóa tài khoản", Logs.UserTarget(user), user.LockReason);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã khóa tài khoản {user.Email}. Người dùng bị đăng xuất và không đăng nhập lại được.";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();

            if (user.IsLocked)
            {
                user.IsLocked = false;
                user.LockReason = null;
                Logs.Audit(_db, Actor, "Mở khóa tài khoản", Logs.UserTarget(user));
                await _db.SaveChangesAsync();
            }
            TempData["Msg"] = $"Đã mở khóa tài khoản {user.Email}.";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        // đặt lại mật khẩu: tạo mật khẩu tạm, hiển thị 1 lần, người dùng phải đổi ở lần đăng nhập sau
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            if (!CanManage(user)) return Forbid();
            if (user.Id == CurrentId)
            {
                TempData["Err"] = "Hãy dùng chức năng \"Đổi mật khẩu\" trong tài khoản của bạn.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var temp = AuthHelper.GeneratePassword();
            user.PasswordHash = _hasher.HashPassword(user, temp);
            user.MustChangePassword = true;
            Logs.Audit(_db, Actor, "Đặt lại mật khẩu", Logs.UserTarget(user));
            await _db.SaveChangesAsync();

            ViewBag.TempPassword = temp;
            return View("ResetResult", user);
        }

        // xóa tài khoản (delete): chỉ Admin, và chỉ khi tài khoản chưa có thẻ thư viện
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin) return Forbid();
            var user = await _db.Users.Include(u => u.Card).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (user.Id == CurrentId)
            {
                TempData["Err"] = "Bạn không thể tự xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (user.Card != null)
            {
                TempData["Err"] = $"Không thể xóa {user.Email} vì tài khoản đã có thẻ thư viện {user.Card.CardNumber}. Hãy khóa tài khoản thay vì xóa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            Logs.Audit(_db, Actor, "Xóa tài khoản", Logs.UserTarget(user));
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã xóa tài khoản {user.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
