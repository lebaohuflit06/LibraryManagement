using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<LibraryCard> Cards => Set<LibraryCard>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanItem> LoanItems => Set<LoanItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<AppUser>().HasOne(u => u.Card).WithOne(c => c.User)
            .HasForeignKey<LibraryCard>(c => c.UserId);

        b.Entity<LibraryCard>().HasIndex(c => c.IdNumber).IsUnique();

        // mỗi mã sách có nhiều cuốn; số kiểm soát không trùng trong cùng 1 mã sách
        b.Entity<BookCopy>().HasOne(c => c.Book).WithMany(x => x.Copies)
            .HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BookCopy>().HasIndex(c => new { c.BookId, c.ControlNo }).IsUnique();

        b.Entity<Loan>().HasOne(l => l.Card).WithMany(c => c.Loans)
            .HasForeignKey(l => l.CardId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LoanItem>().HasOne(i => i.Loan).WithMany(l => l.Items)
            .HasForeignKey(i => i.LoanId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LoanItem>().HasOne(i => i.Copy).WithMany()
            .HasForeignKey(i => i.CopyId).OnDelete(DeleteBehavior.Restrict);
    }
}
