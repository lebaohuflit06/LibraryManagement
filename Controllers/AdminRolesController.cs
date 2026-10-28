using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // phân quyền: gán vai trò Admin / Thủ thư / Thành viên cho tài khoản. Chỉ Admin được vào.
    // Vai trò có hiệu lực ngay ở lần tải trang tiếp theo của người bị đổi (xem OnValidatePrincipal trong Program.cs)
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminRolesController : Controller
    {
        private const int PageSize = 12;
        private readonly AppDbContext _db;
        public AdminRolesController(AppDbContext db) => _db = db;

        private int CurrentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index(string? q, string? role, int page = 1)
        {
            var all = await _db.Users.AsNoTracking()
                .OrderBy(u => u.Role == AppRoles.Admin ? 0 : u.Role == AppRoles.Librarian ? 1 : 2)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            var query = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                query = query.Where(u => TextHelper.Normalize(u.FullName).Contains(key) || TextHelper.Normalize(u.Email).Contains(key));
            }
            var list = query.ToList();
            var totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            return View(new RoleIndexViewModel
            {
                Q = q,
                Role = role,
                Page = page,
                TotalPages = totalPages,
                AdminCount = all.Count(u => u.Role == AppRoles.Admin),
                LibrarianCount = all.Count(u => u.Role == AppRoles.Librarian),
                MemberCount = all.Count(u => u.Role == AppRoles.Member),
                Users = list.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                CurrentUserId = CurrentId
            });
        }

        // đổi vai trò (kèm các ràng buộc để không làm hỏng dữ liệu / tự khóa mình ra khỏi hệ thống)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(int id, string role, string? q, string? filter, int page = 1)
        {
            var back = RedirectToAction(nameof(Index), new { q, role = filter, page });

            if (!AppRoles.All.Contains(role))
            {
                TempData["Err"] = "Vai trò không hợp lệ.";
                return back;
            }
            var user = await _db.Users.Include(u => u.Card).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (user.Id == CurrentId)
            {
                TempData["Err"] = "Bạn không thể tự đổi vai trò của chính mình (tránh mất quyền quản trị). Hãy nhờ một Admin khác.";
                return back;
            }
            if (user.Role == role)
            {
                TempData["Msg"] = $"{user.FullName} đã là {AppRoles.Text(role)}.";
                return back;
            }
            if (user.Role == AppRoles.Admin && await _db.Users.CountAsync(u => u.Role == AppRoles.Admin && u.Id != user.Id) == 0)
            {
                TempData["Err"] = "Hệ thống phải còn ít nhất 1 Admin.";
                return back;
            }

            // thành viên đang mượn sách / còn nợ phí thì chưa chuyển sang nhân viên được
            if (user.Role == AppRoles.Member && AppRoles.IsStaff(role) && user.Card != null)
            {
                var cardId = user.Card.Id;
                if (await _db.Loans.AnyAsync(l => l.CardId == cardId && l.ReturnedAt == null))
                {
                    TempData["Err"] = $"{user.FullName} đang mượn sách, hãy xử lý trả sách trước khi đổi vai trò.";
                    return back;
                }
                if (await _db.Fees.AnyAsync(f => f.CardId == cardId && f.Status == FeeStatus.Unpaid))
                {
                    TempData["Err"] = $"{user.FullName} còn khoản phí chưa thanh toán, hãy xử lý phí trước khi đổi vai trò.";
                    return back;
                }
            }

            var old = user.Role;
            user.Role = role;
            Logs.Audit(_db, User.Identity?.Name, "Đổi vai trò", Logs.UserTarget(user), $"{AppRoles.Text(old)} → {AppRoles.Text(role)}");
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Đã đổi vai trò của {user.FullName}: {AppRoles.Text(old)} → {AppRoles.Text(role)}.";
            return back;
        }

        // nhật ký thao tác (tài khoản, phân quyền, phí...)
        public async Task<IActionResult> Audit(string? q, int page = 1)
        {
            var logs = await _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Id).ToListAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextHelper.Normalize(q);
                logs = logs.Where(a =>
                    TextHelper.Normalize(a.Actor).Contains(key) ||
                    TextHelper.Normalize(a.Action).Contains(key) ||
                    TextHelper.Normalize(a.Target).Contains(key) ||
                    TextHelper.Normalize(a.Detail).Contains(key)).ToList();
            }
            var totalPages = Math.Max(1, (int)Math.Ceiling(logs.Count / 20.0));
            page = Math.Clamp(page, 1, totalPages);
            return View(new AuditViewModel { Q = q, Page = page, TotalPages = totalPages, Logs = logs.Skip((page - 1) * 20).Take(20).ToList() });
        }
    }
}
