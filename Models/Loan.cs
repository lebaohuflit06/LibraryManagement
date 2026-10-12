namespace LibraryManagement.Models
{
    // 1 thẻ, tối đa 3 sách, hạn tối đa 15 ngày
    public class Loan
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public LibraryCard? Card { get; set; }
        public DateTime BorrowedAt { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; } // null nếu chưa trả
        public List<LoanItem> Items { get; set; } = new(); // các đầu sách trong lần mượn này


        // chỉ có get, ko lưu database
        public bool IsReturned => ReturnedAt != null;
        public bool IsOverdue => ReturnedAt == null && DueDate.Date < DateTime.Today;
        public int TotalCompensation => Items.Sum(i => i.Compensation); // vnd
    }

    public class LoanItem
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public Loan? Loan { get; set; }
        public int CopyId { get; set; }
        public BookCopy? Copy { get; set; }
        public BookStatus? ReturnStatus { get; set; } // trạng thái khi trả, null nếu chưa trả, available, lost, damaged
        public string? Note { get; set; }
        public int Compensation { get; set; }
        public bool CompensationPaid { get; set; } = false; // thanh toán bồi thường hay chưa

        // còn nợ
        public bool IsUnpaid => Compensation > 0 && !CompensationPaid;
    }
}

