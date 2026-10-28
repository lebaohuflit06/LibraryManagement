namespace LibraryManagement.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int Year { get; set; }
    public int Pages { get; set; }
    public int Price { get; set; } // vnd, dùng để tính phí bồi thường
    public List<BookCopy> Copies { get; set; } = new();
    public int Quantity => Copies.Count(c => c.Status != BookStatus.Lost && c.Status != BookStatus.Disposed); // tổng số cuốn thuộc đầu sách, trừ các bản sao bị mất / đã thanh lý
    public int Available => Copies.Count(c => c.Status == BookStatus.Available); // tổng số cuốn còn sẵn sàng cho mượn
    public string Description { get; set; } = "";
}
