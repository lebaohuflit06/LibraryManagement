using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Data
{
    public static class DbSeeder
    {
        // tạo tài khoản admin mặc định: admin@gmail.com / Admin@123
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

        // dữ liệu sách mẫu (chỉ nạp khi CSDL chưa có sách nào); mỗi mã sách có sẵn số cuốn tương ứng
        public static void SeedBooks(AppDbContext db)
        {
            if (db.Books.Any()) return;

            var data = new (string Title, string Author, string Category, string Publisher, int Year, int Pages, int Price, int Copies, string Desc)[]
            {
                ("Dế Mèn phiêu lưu ký", "Tô Hoài", "Văn học", "NXB Kim Đồng", 2019, 160, 55000, 5, "Cuộc phiêu lưu của chú Dế Mèn qua nhiều vùng đất, gặp gỡ nhiều bạn bè và rút ra những bài học về lòng dũng cảm, tình bạn."),
                ("Nhà giả kim", "Paulo Coelho", "Văn học", "NXB Hội Nhà Văn", 2020, 228, 79000, 4, "Hành trình đi tìm kho báu của chàng chăn cừu Santiago và ý nghĩa của việc lắng nghe trái tim."),
                ("Lập trình C# căn bản", "Nguyễn Văn An", "Công nghệ", "NXB Thông Tin", 2022, 420, 189000, 6, "Giáo trình nhập môn C#: kiểu dữ liệu, hướng đối tượng, LINQ và xây dựng ứng dụng web cơ bản."),
                ("Cấu trúc dữ liệu và giải thuật", "Trần Minh Khoa", "Công nghệ", "NXB Bách Khoa", 2021, 380, 165000, 3, "Trình bày mảng, danh sách liên kết, cây, đồ thị và các thuật toán sắp xếp, tìm kiếm."),
                ("Sapiens: Lược sử loài người", "Yuval Noah Harari", "Lịch sử", "NXB Tri Thức", 2018, 550, 249000, 4, "Nhìn lại lịch sử loài người từ thời săn bắt hái lượm đến kỷ nguyên công nghệ."),
                ("Đắc nhân tâm", "Dale Carnegie", "Kỹ năng sống", "NXB Tổng Hợp", 2016, 320, 86000, 8, "Những nguyên tắc giao tiếp và ứng xử giúp xây dựng các mối quan hệ tốt đẹp."),
                ("Vũ trụ trong vỏ hạt dẻ", "Stephen Hawking", "Khoa học", "NXB Trẻ", 2017, 250, 120000, 2, "Giới thiệu các lý thuyết vật lý hiện đại về không gian, thời gian và vũ trụ."),
                ("Truyện Kiều", "Nguyễn Du", "Văn học", "NXB Văn Học", 2015, 300, 95000, 3, "Kiệt tác thơ lục bát của văn học Việt Nam kể về cuộc đời nàng Thúy Kiều."),
            };

            foreach (var d in data)
            {
                var book = new Book
                {
                    Title = d.Title, Author = d.Author, Category = d.Category, Publisher = d.Publisher,
                    Year = d.Year, Pages = d.Pages, Price = d.Price, Description = d.Desc
                };
                for (var i = 1; i <= d.Copies; i++)
                    book.Copies.Add(new BookCopy { ControlNo = i, Status = BookStatus.Available });
                db.Books.Add(book);
            }
            db.SaveChanges();
        }
    }
}
