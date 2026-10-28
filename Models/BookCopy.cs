namespace LibraryManagement.Models
{
    public enum BookStatus
    {
        Available = 0, // còn
        Borrowed = 1, // cho mượn
        Lost = 2, // bị mất
        Damaged = 3, // bị hư hỏng
        Disposed = 4 // đã thanh lý (không còn trong kho)
    }
    public class BookCopy // một đầu sách, phân biệt bằng ControlNo
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public Book? Book { get; set; }
        public int ControlNo { get; set; } // số thứ tự của bản sao, dùng để phân biệt các bản sao của cùng một đầu sách}
        public BookStatus Status { get; set; } = BookStatus.Available;
        public string? Note { get; set; }
        public DateTime? StatusChangedAt { get; set; } // thời điểm thay đổi trạng thái gần nhất (sử dụng cho báo cáo)
        public string ControlText => ControlNo.ToString("D3"); // hiển thị ControlNo, 3 chữ số, ví dụ: 001, 002... chỉ có get, ko lưu database
                                                                
    }
}
