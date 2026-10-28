using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Data
{
    public static class DbSeeder
    {
        // tạo tài khoản mặc định (chỉ khi CSDL mới, chưa có admin)
        public static void SeedAdmin(AppDbContext db)
        {
            if (db.Users.Any(u => u.Role == AppRoles.Admin)) return; // đã có admin rồi thì thôi

            var hasher = new PasswordHasher<AppUser>();

            var admin = new AppUser
            {
                FullName = "Quản trị viên",
                Email = "admin@gmail.com",
                Role = AppRoles.Admin
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@123"); // mật khẩu mặc định
            db.Users.Add(admin);

            // thủ thư mẫu để thử phân quyền (admin có thể đổi vai trò / khóa / xóa trong "Hệ thống")
            if (!db.Users.Any(u => u.Role == AppRoles.Librarian))
            {
                var lib = new AppUser
                {
                    FullName = "Thủ thư",
                    Email = "thuthu@gmail.com",
                    Role = AppRoles.Librarian
                };
                lib.PasswordHash = hasher.HashPassword(lib, "Thuthu@123");
                db.Users.Add(lib);
            }
            db.SaveChanges();
        }
    }
}
