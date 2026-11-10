namespace LibraryManagement.Models
{
    public enum BookStatus
    {
        Available = 0,  // còn
        Borrowed = 1,   // cho mượn
        Lost = 2,       // bị mất
        Damaged = 3,    // bị hư hỏng
        Liquidated = 4  // đã thanh lý (loại khỏi thư viện)
    }

    public class BookCopy // một cuốn sách cụ thể, phân biệt bằng ControlNo
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public Book? Book { get; set; }
        public int ControlNo { get; set; } // số kiểm soát, dùng để phân biệt các cuốn của cùng một mã sách
        public BookStatus Status { get; set; } = BookStatus.Available;
        public string? Note { get; set; }
        public DateTime? StatusChangedAt { get; set; } // thời điểm thay đổi trạng thái gần nhất (dùng cho báo cáo)

        public string ControlText => ControlNo.ToString("D3"); // 001, 002... chỉ có get, ko lưu database
    }
}
