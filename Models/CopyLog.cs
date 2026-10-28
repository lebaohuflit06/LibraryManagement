namespace LibraryManagement.Models;

// nhật ký tình trạng của từng cuốn sách: nhập kho, cho mượn, trả, sửa, thanh lý...
public class CopyLog
{
    public int Id { get; set; }
    public int CopyId { get; set; }
    public BookCopy? Copy { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
    public BookStatus? FromStatus { get; set; }   // null khi mới nhập kho
    public BookStatus ToStatus { get; set; }
    public string Action { get; set; } = "";      // nhập kho, cho mượn, trả sách, ...
    public string Actor { get; set; } = "";       // người thực hiện
    public int? LoanId { get; set; }              // phiếu mượn liên quan (nếu có)
    public string? Note { get; set; }
}
