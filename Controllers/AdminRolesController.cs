using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // Quản lý phân quyền: gán quyền Admin / Member cho tài khoản
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminRolesController : Controller
    {
        private readonly AppDbContext _db;
        public AdminRolesController(AppDbContext db) => _db = db;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            ViewBag.CurrentUserId = CurrentUserId;
            var users = await _db.Users.AsNoTracking().OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
            return View(users);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(int id, string role)
        {
            if (role != AppRoles.Admin && role != AppRoles.Member) return BadRequest();

            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();
            if (u.Role == role) return RedirectToAction(nameof(Index));

            if (u.Id == CurrentUserId)
            {
                TempData["Err"] = "Bạn không thể tự đổi quyền của chính mình.";
                return RedirectToAction(nameof(Index));
            }
            // Admin không có thẻ thư viện / phiếu mượn: nếu đã có thẻ thì không cho nâng quyền để tránh dữ liệu mồ côi
            if (role == AppRoles.Admin && await _db.Cards.AnyAsync(c => c.UserId == id))
            {
                TempData["Err"] = $"{u.Email} đã có thẻ thư viện nên không thể chuyển thành quản trị viên.";
                return RedirectToAction(nameof(Index));
            }

            u.Role = role;
            await _db.SaveChangesAsync();
            TempData["Msg"] = $"Đã đổi quyền của {u.Email} thành {LibraryRules.RoleText(role)}. Người này cần đăng nhập lại.";
            return RedirectToAction(nameof(Index));
        }
    }
}
