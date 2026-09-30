using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<LibraryCard> Cards => Set<LibraryCard>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<AppUser>().HasOne(u => u.Card).WithOne(c => c.User)
            .HasForeignKey<LibraryCard>(c => c.UserId);

        b.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Dế Mèn phiêu lưu ký", Author = "Tô Hoài", Category = "Văn học", Publisher = "NXB Kim Đồng", Year = 2019, Pages = 160, Quantity = 5, Available = 5, Description = "Cuộc phiêu lưu của chú Dế Mèn qua nhiều vùng đất, gặp gỡ nhiều bạn bè và rút ra những bài học về lòng dũng cảm, tình bạn." },
            new Book { Id = 2, Title = "Nhà giả kim", Author = "Paulo Coelho", Category = "Văn học", Publisher = "NXB Hội Nhà Văn", Year = 2020, Pages = 228, Quantity = 4, Available = 3, Description = "Hành trình đi tìm kho báu của chàng chăn cừu Santiago và ý nghĩa của việc lắng nghe trái tim." },
            new Book { Id = 3, Title = "Lập trình C# căn bản", Author = "Nguyễn Văn An", Category = "Công nghệ", Publisher = "NXB Thông Tin", Year = 2022, Pages = 420, Quantity = 6, Available = 6, Description = "Giáo trình nhập môn C#: kiểu dữ liệu, hướng đối tượng, LINQ và xây dựng ứng dụng web cơ bản." },
            new Book { Id = 4, Title = "Cấu trúc dữ liệu và giải thuật", Author = "Trần Minh Khoa", Category = "Công nghệ", Publisher = "NXB Bách Khoa", Year = 2021, Pages = 380, Quantity = 3, Available = 2, Description = "Trình bày mảng, danh sách liên kết, cây, đồ thị và các thuật toán sắp xếp, tìm kiếm." },
            new Book { Id = 5, Title = "Sapiens: Lược sử loài người", Author = "Yuval Noah Harari", Category = "Lịch sử", Publisher = "NXB Tri Thức", Year = 2018, Pages = 550, Quantity = 4, Available = 4, Description = "Nhìn lại lịch sử loài người từ thời săn bắt hái lượm đến kỷ nguyên công nghệ." },
            new Book { Id = 6, Title = "Đắc nhân tâm", Author = "Dale Carnegie", Category = "Kỹ năng sống", Publisher = "NXB Tổng Hợp", Year = 2016, Pages = 320, Quantity = 8, Available = 7, Description = "Những nguyên tắc giao tiếp và ứng xử giúp xây dựng các mối quan hệ tốt đẹp." },
            new Book { Id = 7, Title = "Vũ trụ trong vỏ hạt dẻ", Author = "Stephen Hawking", Category = "Khoa học", Publisher = "NXB Trẻ", Year = 2017, Pages = 250, Quantity = 2, Available = 2, Description = "Giới thiệu các lý thuyết vật lý hiện đại về không gian, thời gian và vũ trụ." },
            new Book { Id = 8, Title = "Truyện Kiều", Author = "Nguyễn Du", Category = "Văn học", Publisher = "NXB Văn Học", Year = 2015, Pages = 300, Quantity = 3, Available = 3, Description = "Kiệt tác thơ lục bát của văn học Việt Nam kể về cuộc đời nàng Thúy Kiều." }
        );
    }
}
