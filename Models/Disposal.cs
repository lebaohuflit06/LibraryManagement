namespace LibraryManagement.Models;

public enum DisposalReason { Damaged = 0, Lost = 1, Worn = 2, Old = 3 }                 // hỏng, mất, mượn nhiều (cũ rách), xuất bản đã lâu
public enum DisposalMethod { Destroy = 0, Sell = 1, Donate = 2, WriteOff = 3 }          // tiêu hủy, bán thanh lý, tặng, ghi giảm (sách mất)

// biên bản thanh lý: một lần thanh lý nhiều cuốn
public class DisposalReport
{
    public int Id { get; set; }
    public string Code { get; set; } = "";        // TL2026-001
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "";
    public string? Note { get; set; }
    public int RecoveredAmount { get; set; }      // tiền thu hồi (bán thanh lý)
    public List<DisposalItem> Items { get; set; } = new();

    public int TotalBookValue => Items.Sum(i => i.BookValue);
}

public class DisposalItem
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public DisposalReport? Report { get; set; }
    public int CopyId { get; set; }
    public BookCopy? Copy { get; set; }
    public DisposalReason Reason { get; set; }
    public DisposalMethod Method { get; set; }
    public int BookValue { get; set; }            // giá sách tại thời điểm thanh lý
    public string? Note { get; set; }
}
