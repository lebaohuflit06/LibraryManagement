using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Data
{
    public static class DbSeeder
    {
        // tạo tài khoản admin mặc định
        public static void SeedAdmin(AppDbContext db)
        {
            if (db.Users.Any(u => u.Role == AppRoles.Admin)) return; // đã có admin rồi thì thôi

            var admin = new AppUser
            {
                FullName = "Quản trị viên",
                Email = "admin@gmail.com",
                Role = AppRoles.Admin
            };
            admin.PasswordHash = new PasswordHasher<AppUser>().HashPassword(admin, "Admin@123"); // mật khẩu mặc định
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }
}
