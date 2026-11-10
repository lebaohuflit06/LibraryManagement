using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    // Trang tổng quan dành cho quản trị viên
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var vm = new AdminDashboardViewModel
            {
                BookTitles = await _db.Books.CountAsync(),
                TotalCopies = await _db.BookCopies.CountAsync(c => c.Status != BookStatus.Liquidated),
                AvailableCopies = await _db.BookCopies.CountAsync(c => c.Status == BookStatus.Available),
                BorrowedCopies = await _db.BookCopies.CountAsync(c => c.Status == BookStatus.Borrowed),
                LostDamagedCopies = await _db.BookCopies.CountAsync(c => c.Status == BookStatus.Lost || c.Status == BookStatus.Damaged),
                Members = await _db.Users.CountAsync(u => u.Role == AppRoles.Member),
                Cards = await _db.Cards.CountAsync(),
                ActiveLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null),
                OverdueLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && l.DueDate < today),
                UnpaidFees = await _db.LoanItems.Where(i => i.Compensation > 0 && !i.CompensationPaid).SumAsync(i => (int?)i.Compensation) ?? 0
            };
            return View(vm);
        }
    }
}
