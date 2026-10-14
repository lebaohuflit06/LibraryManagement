using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<LibraryCard> Cards => Set<LibraryCard>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>(); // sprint 2
    public DbSet<Loan> Loans => Set<Loan>(); // sprint 2
    public DbSet<LoanItem> LoanItems => Set<LoanItem>(); // sprint 2

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<AppUser>().HasOne(u => u.Card).WithOne(c => c.User)
            .HasForeignKey<LibraryCard>(c => c.UserId);

        b.Entity<LibraryCard>().HasIndex(c => c.CardNumber).IsUnique(); // mỗi CCCD chỉ được cấp 1 thẻ

        b.Entity<BookCopy>().HasIndex(c => new { c.BookId, c.ControlNo }).IsUnique(); // mỗi bản sao có ControlNo riêng, ko trùng nhau

        // quan hệ 1-n giữa Book và BookCopy, Loan và LoanItem
        b.Entity<BookCopy>().HasOne(c => c.Book).WithMany(bk => bk.Copies)
            .HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasOne(l => l.Card).WithMany(c => c.Loans)
            .HasForeignKey(l => l.CardId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LoanItem>().HasOne(i => i.Loan).WithMany(l => l.Items)
            .HasForeignKey(i => i.LoanId);
        b.Entity<LoanItem>().HasOne(i => i.Copy).WithMany()
            .HasForeignKey(i => i.CopyId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Dế Mèn phiêu lưu ký", Author = "Tô Hoài", Category = "Văn học", Publisher = "NXB Kim Đồng", Year = 2019, Pages = 160, Price = 60000, Description = "Cuộc phiêu lưu của chú Dế Mèn qua nhiều vùng đất, gặp gỡ nhiều bạn bè và rút ra những bài học về lòng dũng cảm, tình bạn." },
            new Book { Id = 2, Title = "Nhà giả kim", Author = "Paulo Coelho", Category = "Văn học", Publisher = "NXB Hội Nhà Văn", Year = 2020, Pages = 228, Price = 79000,  Description = "Hành trình đi tìm kho báu của chàng chăn cừu Santiago và ý nghĩa của việc lắng nghe trái tim." },
            new Book { Id = 3, Title = "Lập trình C# căn bản", Author = "Nguyễn Văn An", Category = "Công nghệ", Publisher = "NXB Thông Tin", Year = 2022, Pages = 420, Price = 120000,  Description = "Giáo trình nhập môn C#: kiểu dữ liệu, hướng đối tượng, LINQ và xây dựng ứng dụng web cơ bản." },
            new Book { Id = 4, Title = "Cấu trúc dữ liệu và giải thuật", Author = "Trần Minh Khoa", Category = "Công nghệ", Publisher = "NXB Bách Khoa", Year = 2021, Pages = 380, Price = 150000,  Description = "Trình bày mảng, danh sách liên kết, cây, đồ thị và các thuật toán sắp xếp, tìm kiếm." },
            new Book { Id = 5, Title = "Sapiens: Lược sử loài người", Author = "Yuval Noah Harari", Category = "Lịch sử", Publisher = "NXB Tri Thức", Year = 2018, Pages = 550, Price = 180000, Description = "Nhìn lại lịch sử loài người từ thời săn bắt hái lượm đến kỷ nguyên công nghệ." },
            new Book { Id = 6, Title = "Đắc nhân tâm", Author = "Dale Carnegie", Category = "Kỹ năng sống", Publisher = "NXB Tổng Hợp", Year = 2016, Pages = 320, Price = 86000,  Description = "Những nguyên tắc giao tiếp và ứng xử giúp xây dựng các mối quan hệ tốt đẹp." },
            new Book { Id = 7, Title = "Vũ trụ trong vỏ hạt dẻ", Author = "Stephen Hawking", Category = "Khoa học", Publisher = "NXB Trẻ", Year = 2017, Pages = 250, Price = 130000, Description = "Giới thiệu các lý thuyết vật lý hiện đại về không gian, thời gian và vũ trụ." },
            new Book { Id = 8, Title = "Truyện Kiều", Author = "Nguyễn Du", Category = "Văn học", Publisher = "NXB Văn Học", Year = 2015, Pages = 300, Price = 70000, Description = "Kiệt tác thơ lục bát của văn học Việt Nam kể về cuộc đời nàng Thúy Kiều." }
        );
        var copyCounts = new Dictionary<int, int> { { 1, 5 }, { 2, 4 }, { 3, 6 }, { 4, 3 }, { 5, 4 }, { 6, 8 }, { 7, 2 }, { 8, 3 } };
        var copies = new List<BookCopy>();
        var copyId = 1;
        foreach (var kv in copyCounts)
            for (var n = 1; n <= kv.Value; n++)
                copies.Add(new BookCopy { Id = copyId++, BookId = kv.Key, ControlNo = n });
        b.Entity<BookCopy>().HasData(copies);
    }
}
