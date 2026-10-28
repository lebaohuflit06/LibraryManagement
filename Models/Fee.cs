namespace LibraryManagement.Models;

public enum FeeType { Late = 0, Damaged = 1, Lost = 2, Other = 3 }       // trả muộn, bồi thường hỏng, bồi thường mất, phí khác
public enum FeeStatus { Unpaid = 0, Paid = 1, Waived = 2 }                // chưa thu, đã thu, đã miễn
public enum PaymentMethod { Cash = 0, Transfer = 1 }                      // tiền mặt, chuyển khoản

// một khoản phí của độc giả (sổ phí). Bồi thường mất/hỏng và phí trả muộn được tạo tự động khi trả sách
public class Fee
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public LibraryCard? Card { get; set; }
    public int? LoanId { get; set; }
    public Loan? Loan { get; set; }
    public int? LoanItemId { get; set; }          // chỉ có với phí bồi thường (gắn với đúng cuốn sách)
    public LoanItem? LoanItem { get; set; }

    public FeeType Type { get; set; }
    public int Amount { get; set; }               // vnd
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public FeeStatus Status { get; set; } = FeeStatus.Unpaid;
    public DateTime? ProcessedAt { get; set; }    // thời điểm thu tiền / miễn
    public PaymentMethod? Method { get; set; }    // hình thức thanh toán (khi đã thu)
    public string? ReceiptNo { get; set; }        // số phiếu thu (khi đã thu)
    public string? ProcessedBy { get; set; }      // nhân viên xử lý
    public string? Note { get; set; }             // ghi chú thu / lý do miễn

    // chỉ có get, ko lưu database
    public bool IsUnpaid => Status == FeeStatus.Unpaid;
    public bool IsCompensation => Type == FeeType.Damaged || Type == FeeType.Lost;
}
