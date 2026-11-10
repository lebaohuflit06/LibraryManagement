using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

// Một MÃ SÁCH (đầu sách theo nghĩa thư mục). Các cuốn thực tế là BookCopy.
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int Year { get; set; }
    public int Pages { get; set; }
    public int Price { get; set; } // giá sách (vnd), dùng tính tiền bồi thường
    public string Description { get; set; } = "";
    public List<BookCopy> Copies { get; set; } = new();

    // chỉ có get, ko lưu database (nhớ Include(b => b.Copies) khi truy vấn)
    [NotMapped] public int Quantity => Copies.Count(c => c.Status != BookStatus.Liquidated); // số cuốn còn thuộc thư viện
    [NotMapped] public int Available => Copies.Count(c => c.Status == BookStatus.Available);  // số cuốn còn trên kệ
}
